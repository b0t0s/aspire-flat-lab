using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal enum LabGroup
{
    Chat,
    Media,
    Secrets,
    Knowledge,
    SmartHome,
    Development,
    Entertainment,
    Irl,
}

internal enum LabMode
{
    Off,
    Manual,
    Auto,
}

internal enum LabRole
{
    Main,
    Alt,
}

internal sealed record LabMembershipAnnotation(LabGroup Group, LabRole Role) : IResourceAnnotation;

internal sealed record LabDefaultModeAnnotation(LabMode Mode) : IResourceAnnotation;

internal sealed class LabGroupResource(string name) : Resource(name);

internal static class LabGroups
{
    public static IDistributedApplicationBuilder AddLabGroup(this IDistributedApplicationBuilder builder, LabGroup group, LabRole role,
        Action<IDistributedApplicationBuilder> register)
    {
        var before = builder.Resources.ToHashSet();
        register(builder);
        foreach (var container in builder.Resources.OfType<ContainerResource>().Where(r => !before.Contains(r)))
        {
            container.Annotations.Add(new LabMembershipAnnotation(group, role));
        }
        return builder;
    }

    public static IResourceBuilder<T> WithDefaultMode<T>(this IResourceBuilder<T> resource, LabMode mode)
        where T : ContainerResource
    {
        resource.Resource.Annotations.Add(new LabDefaultModeAnnotation(mode));
        return resource;
    }

    public static IDistributedApplicationBuilder ApplyLabModes(this IDistributedApplicationBuilder builder)
    {
        var containers = builder.Resources.OfType<ContainerResource>().ToList();
        var waitersOf = containers
            .SelectMany(waiter => waiter.Annotations.OfType<WaitAnnotation>().Select(wait => (Dependency: wait.Resource, Waiter: waiter)))
            .ToLookup(pair => pair.Dependency, pair => pair.Waiter);

        LabMode? Setting(string key)
        {
            var value = builder.Configuration[key];
            if (string.IsNullOrWhiteSpace(value)) return null;
            return Enum.TryParse<LabMode>(value, ignoreCase: true, out var mode)
                ? mode
                : throw new InvalidOperationException($"{key}={value}: expected one of {string.Join(", ", Enum.GetNames<LabMode>()).ToLowerInvariant()}");
        }

        static string Key(string name)
        {
            return name.ToUpperInvariant().Replace('-', '_');
        }

        var modes = new Dictionary<ContainerResource, LabMode>();
        LabMode Resolve(ContainerResource container)
        {
            if (modes.TryGetValue(container, out var known)) return known;
            modes[container] = LabMode.Manual;

            var mode = Setting($"FLAT_LAB_SERVICE_{Key(container.Name)}") ?? (container.Annotations.OfType<LabMembershipAnnotation>().FirstOrDefault() switch
            {
                { } own when Setting($"FLAT_LAB_GROUP_{Key(own.Group.ToString())}") == LabMode.Off => LabMode.Off,
                { Role: LabRole.Alt } own => Setting($"FLAT_LAB_GROUP_{Key(own.Group.ToString())}_ALTS") ?? LabMode.Off,
                { } own => Setting($"FLAT_LAB_GROUP_{Key(own.Group.ToString())}")
                    ?? container.Annotations.OfType<LabDefaultModeAnnotation>().LastOrDefault()?.Mode
                    ?? LabMode.Manual,
                null => waitersOf[container].Any() && waitersOf[container].All(waiter => Resolve(waiter) == LabMode.Off) ? LabMode.Off : LabMode.Manual,
            });
            modes[container] = mode;
            return mode;
        }

        foreach (var container in containers) _ = Resolve(container);

        // A dependency follows its services: off once every service waiting for it is off, unless it has its own key or code default.
        bool changed;
        do
        {
            changed = false;
            foreach (var container in containers.Where(c => modes[c] != LabMode.Off && waitersOf[c].Any()))
            {
                if (Setting($"FLAT_LAB_SERVICE_{Key(container.Name)}") is not null || container.Annotations.OfType<LabDefaultModeAnnotation>().Any()) continue;
                if (!waitersOf[container].All(waiter => modes[waiter] == LabMode.Off)) continue;
                modes[container] = LabMode.Off;
                changed = true;
            }
        }
        while (changed);

        var needed = new Stack<ContainerResource>(containers.Where(c => modes[c] != LabMode.Off));
        while (needed.TryPop(out var container))
        {
            foreach (var dependency in container.Annotations.OfType<WaitAnnotation>().Select(w => w.Resource).OfType<ContainerResource>())
            {
                if (modes[dependency] != LabMode.Off) continue;
                Console.WriteLine($"[flat-lab] {dependency.Name} is off but {container.Name} waits for it; keeping it (manual)");
                modes[dependency] = LabMode.Manual;
                needed.Push(dependency);
            }
        }

        foreach (var (container, mode) in modes)
        {
            switch (mode)
            {
                case LabMode.Off:
                    _ = builder.Resources.Remove(container);
                    break;
                case LabMode.Manual:
                    _ = builder.CreateResourceBuilder(container).WithExplicitStart();
                    break;
                case LabMode.Auto:
                    break;
            }
        }

        if (builder.ExecutionContext.IsRunMode)
        {
            AddDashboardTree(builder, containers.Where(c => modes[c] != LabMode.Off).ToList(), waitersOf);
        }

        return builder;
    }

    private static void AddDashboardTree(IDistributedApplicationBuilder builder, List<ContainerResource> containers,
        ILookup<IResource, ContainerResource> waitersOf)
    {
        var kept = containers.ToHashSet();
        var parents = new Dictionary<ContainerResource, IResource>();
        var groupOf = new Dictionary<ContainerResource, LabGroup>();
        var nodes = new Dictionary<LabGroup, IResourceBuilder<LabGroupResource>>();

        LabGroup? GroupOf(ContainerResource container, HashSet<ContainerResource> path)
        {
            if (groupOf.TryGetValue(container, out var known)) return known;
            if (!path.Add(container)) return null;
            var group = container.Annotations.OfType<LabMembershipAnnotation>().FirstOrDefault()?.Group
                ?? waitersOf[container].Where(kept.Contains).Select(waiter => GroupOf(waiter, path)).FirstOrDefault(g => g is not null);
            if (group is { } found) groupOf[container] = found;
            return group;
        }

        foreach (var container in containers)
        {
            if (GroupOf(container, []) is not { } group) continue;
            if (!nodes.TryGetValue(group, out var node))
            {
                node = AddGroupNode(builder, group, parents);
                nodes[group] = node;
            }

            IResource parent = waitersOf[container].FirstOrDefault(kept.Contains) is { } waiter ? waiter : node.Resource;
            parents[container] = parent;
            _ = builder.CreateResourceBuilder(container).WithParentRelationship(parent);
        }

        foreach (var container in containers)
        {
            var dependencies = container.Annotations.OfType<WaitAnnotation>().Select(w => w.Resource).OfType<ContainerResource>().ToList();
            if (dependencies.Count == 0) continue;
            _ = builder.Eventing.Subscribe<BeforeResourceStartedEvent>(container, (@event, cancellationToken) =>
            {
                var commands = @event.Services.GetRequiredService<ResourceCommandService>();
                foreach (var dependency in dependencies.Where(d => d.Annotations.OfType<ExplicitStartupAnnotation>().Any()))
                {
                    _ = commands.ExecuteCommandAsync(dependency, KnownResourceCommands.StartCommand, CancellationToken.None);
                }
                return Task.CompletedTask;
            });
        }

        _ = builder.Eventing.Subscribe<BeforeStartEvent>((@event, cancellationToken) =>
        {
            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();
            var stopping = @event.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            _ = Task.Run(() => WatchGroupStatesAsync(notifications, parents, stopping), stopping);
            return Task.CompletedTask;
        });
    }

    private static IResourceBuilder<LabGroupResource> AddGroupNode(IDistributedApplicationBuilder builder, LabGroup group,
        Dictionary<ContainerResource, IResource> parents)
    {
        IEnumerable<ContainerResource> ChildrenOf(IResource parent)
        {
            return parents.Where(p => p.Value == parent).Select(p => p.Key);
        }

        async Task<ExecuteCommandResult> RunAsync(ExecuteCommandContext context, string command, IEnumerable<ContainerResource> targets)
        {
            var commands = context.Services.GetRequiredService<ResourceCommandService>();
            _ = await Task.WhenAll(targets.Select(t => commands.ExecuteCommandAsync(t, command, context.CancellationToken)));
            return CommandResults.Success();
        }

        var node = builder.AddResource(new LabGroupResource(group.ToString().ToLowerInvariant()))
            .ExcludeFromManifest()
            .WithIconName("Folder")
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "Group",
                State = new ResourceStateSnapshot("0 running", KnownResourceStateStyles.Info),
                Properties = [],
            });

        return node
            .WithCommand("start-group", "Start all", context => RunAsync(context, KnownResourceCommands.StartCommand, ChildrenOf(node.Resource)),
                new CommandOptions { IconName = "Play", IsHighlighted = true, UpdateState = _ => ResourceCommandState.Enabled })
            .WithCommand("stop-group", "Stop all", async context =>
            {
                var level = ChildrenOf(node.Resource).ToList();
                while (level.Count > 0)
                {
                    _ = await RunAsync(context, KnownResourceCommands.StopCommand, level);
                    level = level.SelectMany(ChildrenOf).ToList();
                }
                return CommandResults.Success();
            }, new CommandOptions { IconName = "Stop", ConfirmationMessage = $"Stop every service in {group}?", UpdateState = _ => ResourceCommandState.Enabled });
    }

    private static async Task WatchGroupStatesAsync(ResourceNotificationService notifications,
        Dictionary<ContainerResource, IResource> parents, CancellationToken cancellationToken)
    {
        LabGroupResource NodeOf(ContainerResource container)
        {
            var parent = parents[container];
            return parent as LabGroupResource ?? NodeOf((ContainerResource)parent);
        }

        var nodeOf = parents.Keys.ToDictionary(c => (IResource)c, NodeOf);
        var states = new Dictionary<IResource, string?>();
        var published = new Dictionary<LabGroupResource, string>();

        await foreach (var change in notifications.WatchAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!nodeOf.TryGetValue(change.Resource, out var node)) continue;
            states[change.Resource] = change.Snapshot.State?.Text;

            var members = nodeOf.Where(p => p.Value == node).Select(p => p.Key).ToList();
            var running = members.Count(m => states.GetValueOrDefault(m) == KnownResourceStates.Running);
            var failed = members.Count(m => states.GetValueOrDefault(m) == KnownResourceStates.FailedToStart);
            var text = failed > 0 ? $"{running} of {members.Count} running, {failed} failed" : $"{running} of {members.Count} running";
            if (published.GetValueOrDefault(node) == text) continue;
            published[node] = text;

            var style = failed > 0 ? KnownResourceStateStyles.Error : running > 0 ? KnownResourceStateStyles.Success : KnownResourceStateStyles.Info;
            await notifications.PublishUpdateAsync(node, s => s with { State = new ResourceStateSnapshot(text, style) }).ConfigureAwait(false);
        }
    }
}
