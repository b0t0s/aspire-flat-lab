global using static ContainerConventions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.Eventing;

/// <summary>
/// Container settings that must reach both run mode (DCP, as <c>docker run</c> flags)
/// and publish mode (Docker Compose service properties).
/// Plain <c>WithContainerRuntimeArgs</c> only affects run mode, so every lab-wide
/// convention is recorded here and emitted twice by <see cref="ContainerConventions.ApplyLabConventions"/>.
/// </summary>
public sealed class LabContainerAnnotation : IResourceAnnotation
{
    public const string DefaultNetwork = "flat_lab_net";

    public string Restart { get; set; } = "unless-stopped";
    public string Network { get; set; } = DefaultNetwork;
    public Dictionary<string, string> Labels { get; } = new(StringComparer.Ordinal);
    public List<string> Dns { get; } = [];
    public List<string> CapAdd { get; } = [];
    public List<string> Devices { get; } = [];
    public List<string> Tmpfs { get; } = [];
    public List<string> EnvFiles { get; } = [];
    public List<(int HostPort, int ContainerPort, string Protocol)> ExtraPorts { get; } = [];
    public bool Privileged { get; set; }
    public bool ReadOnly { get; set; }
    public string? Pid { get; set; }
    public string? User { get; set; }
    public string? ShmSize { get; set; }
    public string? Hostname { get; set; }
    public LabHealthcheck? Healthcheck { get; set; }

    /// <summary>Web ports whose host port is generated from the group prefix (see <see cref="ContainerConventions.WithLabPort"/>).</summary>
    public List<(int TargetPort, string Name, string? Scheme)> LabPorts { get; } = [];

    /// <summary>Port group when the resource has no homepage entry; defaults to its homepage group.</summary>
    public string? PortGroup { get; set; }

    public bool IsHostNetwork => Network == "host";
}

public sealed record LabHealthcheck(string Command, string Interval = "10s", string Timeout = "5s", int Retries = 10, string? StartPeriod = null);

public static class ContainerConventions
{
    public static LabContainerAnnotation GetLabSettings(this IResource resource)
    {
        var settings = resource.Annotations.OfType<LabContainerAnnotation>().FirstOrDefault();
        if (settings is null)
        {
            settings = new LabContainerAnnotation();
            resource.Annotations.Add(settings);
        }

        return settings;
    }

    public static IResourceBuilder<T> WithLab<T>(this IResourceBuilder<T> resource, Action<LabContainerAnnotation> configure)
        where T : ContainerResource
    {
        configure(resource.Resource.GetLabSettings());
        return resource;
    }

    public static IResourceBuilder<T> WithStartPolicy<T>(this IResourceBuilder<T> resource, bool startByDefault)
        where T : ContainerResource =>
        startByDefault ? resource : resource.WithExplicitStart();

    public static IResourceBuilder<T> WithRestart<T>(this IResourceBuilder<T> resource, string policy)
        where T : ContainerResource => resource.WithLab(lab => lab.Restart = policy);

    public static IResourceBuilder<T> WithNetwork<T>(this IResourceBuilder<T> resource, string network)
        where T : ContainerResource => resource.WithLab(lab => lab.Network = network);

    public static IResourceBuilder<T> WithHostNetwork<T>(this IResourceBuilder<T> resource)
        where T : ContainerResource => resource.WithNetwork("host");

    public static IResourceBuilder<T> WithDns<T>(this IResourceBuilder<T> resource, params string?[] servers)
        where T : ContainerResource =>
        resource.WithLab(lab => lab.Dns.AddRange(servers.Where(s => !string.IsNullOrWhiteSpace(s))!));

    public static IResourceBuilder<T> WithEnvFile<T>(this IResourceBuilder<T> resource, string path)
        where T : ContainerResource => resource.WithLab(lab => lab.EnvFiles.Add(path));

    /// <summary>
    /// A web/UI port. The host port is generated: group prefix * 1000 + position of the port within its group,
    /// in declaration order (docs/ports.md). Add new services at the end of their group to keep existing ports stable.
    /// Standard protocol ports (IRC 6667, XMPP 5222, ...) use a plain <c>WithEndpoint(port, port)</c> instead.
    /// </summary>
    public static IResourceBuilder<T> WithLabPort<T>(this IResourceBuilder<T> resource, int targetPort, string name, string? scheme = null)
        where T : ContainerResource => resource.WithLab(lab => lab.LabPorts.Add((targetPort, name, scheme)));

    /// <summary>Publish a raw host port (e.g. UDP) that is not modelled as an Aspire endpoint.</summary>
    public static IResourceBuilder<T> WithPublishedPort<T>(this IResourceBuilder<T> resource, int hostPort, int containerPort, string protocol = "tcp")
        where T : ContainerResource => resource.WithLab(lab => lab.ExtraPorts.Add((hostPort, containerPort, protocol)));

    public static IResourceBuilder<T> WithHealthcheck<T>(this IResourceBuilder<T> resource, string command,
        string interval = "10s", string timeout = "5s", int retries = 10, string? startPeriod = null)
        where T : ContainerResource =>
        resource.WithLab(lab => lab.Healthcheck = new LabHealthcheck(command, interval, timeout, retries, startPeriod));

    /// <summary>gethomepage.dev docker-label discovery.</summary>
    public static IResourceBuilder<T> WithHomepage<T>(this IResourceBuilder<T> resource,
        string group, string name, string icon, string description = "", string? href = null)
        where T : ContainerResource =>
        resource.WithLab(lab =>
        {
            lab.Labels["homepage.group"] = group;
            lab.Labels["homepage.name"] = name;
            lab.Labels["homepage.icon"] = icon;
            lab.Labels["homepage.description"] = description;
            if (href is not null) lab.Labels["homepage.href"] = href;
        });

    public static IResourceBuilder<T> WithHomepageWidget<T>(this IResourceBuilder<T> resource,
        string type, string? url = null,
        string? key = null, string? username = null, string? password = null, string? token = null, string? salt = null, string? version = null)
        where T : ContainerResource =>
        resource.WithLab(lab =>
        {
            lab.Labels["homepage.widget.type"] = type;
            if (url is not null) lab.Labels["homepage.widget.url"] = url;
            foreach (var (field, value) in new[] { ("key", key), ("username", username), ("password", password), ("token", token), ("salt", salt), ("version", version) })
            {
                if (!string.IsNullOrEmpty(value)) lab.Labels[$"homepage.widget.{field}"] = value;
            }
        });

    /// <summary>
    /// Homepage substitutes <c>{{HOMEPAGE_VAR_*}}</c> from its own environment, so widget credentials stay out of
    /// container labels. The homepage service in docker-compose.yml maps <c>HOMEPAGE_VAR_KEY=${KEY}</c>.
    /// </summary>
    public static string HomepageVar(string envKey) => $"{{{{HOMEPAGE_VAR_{envKey}}}}}";

    /// <summary>Dashboard button that runs a shell command inside the running container.</summary>
    public static IResourceBuilder<T> WithDockerExecCommand<T>(this IResourceBuilder<T> resource, string name, string displayName, string shellCommand, string icon = "ArrowSync")
        where T : ContainerResource =>
        resource.WithCommand(name, displayName, async context =>
        {
            var containerName = resource.Resource.Annotations.OfType<ContainerNameAnnotation>().LastOrDefault()?.Name ?? resource.Resource.Name;
            var start = new System.Diagnostics.ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "exec", containerName, "sh", "-c", shellCommand }) start.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync(context.CancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(context.CancellationToken);
            await process.WaitForExitAsync(context.CancellationToken);
            return process.ExitCode == 0
                ? CommandResults.Success()
                : CommandResults.Failure($"exit {process.ExitCode}: {await stderr}{await stdout}");
        }, new CommandOptions
        {
            IconName = icon,
            Description = shellCommand,
            UpdateState = context => context.ResourceSnapshot.State?.Text == KnownResourceStates.Running
                ? ResourceCommandState.Enabled
                : ResourceCommandState.Disabled,
        });

    public static IResourceBuilder<T> WithDockerSocket<T>(this IResourceBuilder<T> resource, bool isReadOnly = true)
        where T : ContainerResource =>
        resource.WithBindMount("/var/run/docker.sock", "/var/run/docker.sock", isReadOnly);

    /// <summary>Bind-mount a path that lives in this repository (relative to <c>apphost.cs</c>).</summary>
    public static IResourceBuilder<T> WithRepoMount<T>(this IResourceBuilder<T> resource,
        string repoRelativePath, string containerPath, bool isReadOnly = false)
        where T : ContainerResource =>
        resource.WithBindMount(RepoPath(resource.ApplicationBuilder, repoRelativePath), containerPath, isReadOnly);

    public static string RepoPath(this IDistributedApplicationBuilder builder, string repoRelativePath) =>
        Path.GetFullPath(repoRelativePath, builder.AppHostDirectory);

    /// <summary>
    /// A human-edited config file owned by the repo (configs/...), mounted read-only.
    /// If the repo copy does not exist yet but the service's old copy does (<paramref name="legacyHostPath"/>, usually under
    /// services-data), it is imported into configs/ on the first run so it can be reviewed and committed.
    /// With neither present the mount is skipped and the image's built-in config applies.
    /// </summary>
    public static IResourceBuilder<T> WithConfigFile<T>(this IResourceBuilder<T> resource, string repoRelativePath, string containerPath, string? legacyHostPath = null)
        where T : ContainerResource
    {
        var builder = resource.ApplicationBuilder;
        var source = builder.RepoPath(repoRelativePath);
        if (builder.ExecutionContext.IsRunMode && !File.Exists(source))
        {
            if (legacyHostPath is not null && File.Exists(legacyHostPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(source)!);
                File.Copy(legacyHostPath, source);
                Console.WriteLine($"[flat-lab] imported {legacyHostPath} -> {repoRelativePath} (review and commit it)");
            }
            else
            {
                Console.WriteLine($"[flat-lab] {resource.Resource.Name}: {repoRelativePath} not found, using the image's built-in config");
                return resource;
            }
        }

        return resource.WithBindMount(source, containerPath, isReadOnly: true);
    }

    /// <summary>
    /// A config file that needs secrets: configs/&lt;template&gt; with <c>${NAME}</c> placeholders is rendered at container start and
    /// copied to <paramref name="containerDirectory"/>. Values are strings or secret parameters; in publish mode parameters stay
    /// <c>${ENV_KEY}</c> so compose fills them from .env and no secret is written anywhere else.
    /// </summary>
    public static IResourceBuilder<T> WithConfigTemplate<T>(this IResourceBuilder<T> resource, string repoTemplatePath, string containerDirectory, string fileName,
        IReadOnlyDictionary<string, object> variables, Func<string, string>? escape = null, int? owner = null)
        where T : ContainerResource
    {
        var builder = resource.ApplicationBuilder;
        var template = builder.RepoPath(repoTemplatePath);
        return resource.WithContainerFiles(containerDirectory, async (_, cancellationToken) =>
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, value) in variables)
            {
                values[name] = value switch
                {
                    IResourceBuilder<ParameterResource> parameter when builder.ExecutionContext.IsPublishMode =>
                        "${" + parameter.Resource.Name.ToUpperInvariant().Replace('-', '_') + "}",
                    IResourceBuilder<ParameterResource> parameter =>
                        (escape ?? (s => s))(await parameter.Resource.GetValueAsync(cancellationToken) ?? ""),
                    _ => (escape ?? (s => s))(value.ToString() ?? ""),
                };
            }

            var contents = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(template), @"\$\{([A-Z0-9_]+)\}",
                match => values.TryGetValue(match.Groups[1].Value, out var rendered) ? rendered : match.Value);
            return [new ContainerFile { Name = fileName, Contents = contents }];
        }, defaultOwner: owner, defaultGroup: owner);
    }

    /// <summary>
    /// Cascade auto-start through the dependency graph: anything an auto-started resource
    /// <c>WaitFor</c>s must auto-start as well, otherwise the consumer hangs in "Waiting".
    /// </summary>
    public static IDistributedApplicationBuilder CascadeAutoStartThroughDependencies(this IDistributedApplicationBuilder builder)
    {
        builder.Eventing.Subscribe<BeforeStartEvent>((@event, _) =>
        {
            var pending = new Stack<IResource>(@event.Model.Resources
                .Where(r => !r.Annotations.OfType<ExplicitStartupAnnotation>().Any()));
            var visited = new HashSet<IResource>();
            while (pending.TryPop(out var resource))
            {
                if (!visited.Add(resource)) continue;
                foreach (var wait in resource.Annotations.OfType<WaitAnnotation>())
                {
                    var dependency = wait.Resource;
                    foreach (var explicitStart in dependency.Annotations.OfType<ExplicitStartupAnnotation>().ToList())
                    {
                        dependency.Annotations.Remove(explicitStart);
                    }
                    pending.Push(dependency);
                }
            }
            return Task.CompletedTask;
        });
        return builder;
    }

    /// <summary>
    /// Emit every <see cref="LabContainerAnnotation"/> into both run mode and publish mode.
    /// Call once, after all resources are added and before <c>Build()</c>.
    /// </summary>
    public static IDistributedApplicationBuilder ApplyLabConventions(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DockerComposeEnvironmentResource> compose,
        string dataRoot)
    {
        WritePortsDocument(builder, AssignHostPorts(builder));

        var publishDirectory = Path.GetFullPath(
            builder.Configuration["Pipeline:OutputPath"] ?? builder.Configuration["Publishing:OutputPath"] ?? "aspire-output",
            builder.AppHostDirectory);

        compose
            .WithProperties(env => env.DefaultNetworkName = LabContainerAnnotation.DefaultNetwork)
            .ConfigureComposeFile(file =>
            {
                // flat_lab_net is owned by docker-compose.yml (infra layer); the app stack only joins it.
                if (file.Networks.TryGetValue(LabContainerAnnotation.DefaultNetwork, out var network))
                {
                    network.Name = LabContainerAnnotation.DefaultNetwork;
                    network.External = true;
                    network.Driver = null;
                }
            })
            .ConfigureEnvFile(env =>
            {
                // Bind mounts and Dockerfile images are rewritten below, so their generated placeholders are unused.
                foreach (var key in env.Keys.Where(k => k.Contains("_BINDMOUNT_", StringComparison.Ordinal)).ToList())
                {
                    env.Remove(key);
                }
            });

        foreach (var container in builder.Resources.OfType<ContainerResource>().ToList())
        {
            if (container is DockerComposeAspireDashboardResource) continue;

            var lab = container.GetLabSettings();
            var resource = builder.CreateResourceBuilder(container);

            resource.WithContainerRuntimeArgs(context =>
            {
                var args = context.Args;
                args.Add($"--restart={lab.Restart}");
                args.Add($"--network={lab.Network}");
                foreach (var (key, value) in lab.Labels) args.Add($"--label={key}={value}");
                foreach (var dns in lab.Dns) args.Add($"--dns={dns}");
                foreach (var cap in lab.CapAdd) args.Add($"--cap-add={cap}");
                foreach (var device in lab.Devices) args.Add($"--device={device}");
                foreach (var tmpfs in lab.Tmpfs) args.Add($"--tmpfs={tmpfs}");
                foreach (var envFile in lab.EnvFiles) args.Add($"--env-file={envFile}");
                foreach (var (host, target, protocol) in lab.ExtraPorts) args.Add($"--publish={host}:{target}/{protocol}");
                if (lab.Privileged) args.Add("--privileged");
                if (lab.ReadOnly) args.Add("--read-only");
                if (lab.Pid is not null) args.Add($"--pid={lab.Pid}");
                if (lab.User is not null) args.Add($"--user={lab.User}");
                if (lab.ShmSize is not null) args.Add($"--shm-size={lab.ShmSize}");
                if (lab.Hostname is not null) args.Add($"--hostname={lab.Hostname}");
                if (lab.Healthcheck is { } health)
                {
                    args.Add($"--health-cmd={health.Command}");
                    args.Add($"--health-interval={health.Interval}");
                    args.Add($"--health-timeout={health.Timeout}");
                    args.Add($"--health-retries={health.Retries}");
                    if (health.StartPeriod is not null) args.Add($"--health-start-period={health.StartPeriod}");
                }
            });

            resource.PublishAsDockerComposeService((_, service) =>
            {
                service.Restart = lab.Restart;
                if (lab.IsHostNetwork)
                {
                    service.NetworkMode = "host";
                    service.Networks.Clear();
                }
                else if (!service.Networks.Contains(lab.Network))
                {
                    service.Networks.Clear();
                    service.Networks.Add(lab.Network);
                }

                // Literal values (e.g. passwords containing '$') must not be interpolated by compose.
                foreach (var key in service.Environment.Keys.ToList())
                {
                    if (service.Environment[key] is { } value) service.Environment[key] = EscapeForCompose(value);
                }

                foreach (var (key, value) in lab.Labels) service.Labels[key] = EscapeForCompose(value);
                service.Dns.AddRange(lab.Dns);
                service.CapAdd.AddRange(lab.CapAdd);
                service.Devices.AddRange(lab.Devices);
                service.Tmpfs.AddRange(lab.Tmpfs);
                service.EnvFile.AddRange(lab.EnvFiles.Select(path => ToComposePath(path, publishDirectory, builder.AppHostDirectory, dataRoot)));
                if (lab.Privileged) service.Privileged = true;
                if (lab.ReadOnly) service.ReadOnly = true;
                service.Pid = lab.Pid ?? service.Pid;
                service.User = lab.User ?? service.User;
                service.ShmSize = lab.ShmSize ?? service.ShmSize;
                service.Hostname = lab.Hostname ?? service.Hostname;
                if (lab.Healthcheck is { } health)
                {
                    service.Healthcheck = new Healthcheck
                    {
                        Test = ["CMD-SHELL", EscapeForCompose(health.Command)],
                        Interval = health.Interval,
                        Timeout = health.Timeout,
                        Retries = health.Retries,
                        StartPeriod = health.StartPeriod!,
                    };
                }

                // Host ports: mirror run mode, where every endpoint with an explicit port is published on the host.
                if (!lab.IsHostNetwork)
                {
                    foreach (var endpoint in container.Annotations.OfType<EndpointAnnotation>())
                    {
                        if (endpoint.Port is not { } hostPort || endpoint.TargetPort is not { } targetPort) continue;
                        service.Ports.Add($"{hostPort}:{targetPort}/{(endpoint.Protocol == System.Net.Sockets.ProtocolType.Udp ? "udp" : "tcp")}");
                    }
                    foreach (var (host, target, protocol) in lab.ExtraPorts) service.Ports.Add($"{host}:{target}/{protocol}");
                }
                else
                {
                    service.Expose.Clear();
                }

                // Bind mounts: replace the empty host-specific placeholders with paths relative to the compose file.
                var mounts = container.Annotations.OfType<ContainerMountAnnotation>()
                    .Where(m => m.Type == ContainerMountType.BindMount && m.Source is not null)
                    .ToDictionary(m => m.Target, m => m.Source!, StringComparer.Ordinal);
                foreach (var volume in service.Volumes)
                {
                    if (volume.Target is not null && mounts.TryGetValue(volume.Target, out var source))
                    {
                        volume.Source = ToComposePath(source, publishDirectory, builder.AppHostDirectory, dataRoot);
                    }
                }

                // Dockerfile resources: build from the repo instead of expecting a pre-pushed ${X_IMAGE}.
                if (container.Annotations.OfType<DockerfileBuildAnnotation>().FirstOrDefault() is { } build)
                {
                    service.Image = $"flat-lab/{container.Name}:local";
                    service.Build = new Build
                    {
                        Context = ToComposePath(build.ContextPath, publishDirectory, builder.AppHostDirectory, dataRoot),
                        Dockerfile = Path.GetRelativePath(build.ContextPath, build.DockerfilePath).Replace('\\', '/'),
                    };
                }
            });
        }

        return builder;
    }

    /// <summary>
    /// Port groups = homepage groups. Host port = prefix * 1000 + offset + position within the group (declaration order).
    /// A prefix can be moved with <c>PORT_PREFIX_&lt;GROUP&gt;</c> in .env; nothing else about ports is configurable.
    /// 100xx is shared with docker-compose.yml, which owns 10000-10099, so the app stack starts at 10100 there.
    /// </summary>
    public static readonly IReadOnlyList<(string Group, int Prefix, int Offset)> PortGroups =
    [
        ("Infrastructure", 10, 100),
        ("Development", 11, 0),
        ("Knowledge", 12, 0),
        ("Media", 13, 0),
        ("Entertainment", 14, 0),
        ("Household", 15, 0),
        ("Secrets", 16, 0),
        ("Messaging", 17, 0),
        ("Irl", 18, 0),
        // 19xxx: Aspire dashboard / OTLP / resource service (apphost.run.json)
    ];

    private sealed record PortRow(string Group, int HostPort, string Protocol, string Resource, string Container, int TargetPort, string Endpoint);

    private static List<PortRow> AssignHostPorts(IDistributedApplicationBuilder builder)
    {
        var rows = new List<PortRow>();
        var basePort = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (group, defaultPrefix, offset) in PortGroups)
        {
            var prefix = int.TryParse(builder.Configuration[$"PORT_PREFIX_{group.ToUpperInvariant()}"], out var configured) ? configured : defaultPrefix;
            basePort[group] = prefix * 1000 + offset;
        }

        // Sticky slots: configs/ports.json remembers each endpoint's position in its group, so inserting a
        // service anywhere only takes the lowest free slot and never shifts existing ports.
        var lockPath = Path.Combine(builder.AppHostDirectory, "configs", "ports.json");
        var previous = File.Exists(lockPath)
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(File.ReadAllText(lockPath)) ?? []
            : [];
        var slots = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);

        var declared = builder.Resources.OfType<ContainerResource>()
            .SelectMany(container =>
            {
                var lab = container.GetLabSettings();
                var group = lab.PortGroup ?? (lab.Labels.TryGetValue("homepage.group", out var label) ? label : null);
                if (lab.LabPorts.Count > 0 && (group is null || !basePort.ContainsKey(group)))
                {
                    throw new InvalidOperationException(
                        $"'{container.Name}' declares WithLabPort but has no port group; use WithHomepage(group, ...) or WithLab(lab => lab.PortGroup = ...). Known groups: {string.Join(", ", basePort.Keys)}");
                }
                return lab.LabPorts.Select(p => (Container: container, Group: group!, Key: $"{container.Name}/{p.Name}", p.TargetPort, p.Name, p.Scheme));
            })
            .ToList();

        foreach (var byGroup in declared.GroupBy(d => d.Group))
        {
            var known = previous.GetValueOrDefault(byGroup.Key) ?? [];
            var groupSlots = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in byGroup.Where(e => known.ContainsKey(e.Key))) groupSlots[entry.Key] = known[entry.Key];
            foreach (var entry in byGroup.Where(e => !known.ContainsKey(e.Key)))
            {
                var used = groupSlots.Values.ToHashSet();
                groupSlots[entry.Key] = Enumerable.Range(0, 1000).First(slot => !used.Contains(slot));
            }
            slots[byGroup.Key] = groupSlots;

            foreach (var entry in byGroup)
            {
                var port = basePort[byGroup.Key] + groupSlots[entry.Key];
                builder.CreateResourceBuilder(entry.Container)
                    .WithEndpoint(port: port, targetPort: entry.TargetPort, name: entry.Name, scheme: entry.Scheme ?? "http");
                var containerName = entry.Container.Annotations.OfType<ContainerNameAnnotation>().LastOrDefault()?.Name ?? entry.Container.Name;
                rows.Add(new PortRow(byGroup.Key, port, "tcp", entry.Container.Name, containerName, entry.TargetPort, entry.Name));
            }
        }

        var lockContent = System.Text.Json.JsonSerializer.Serialize(slots, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n";
        WriteIfChanged(lockPath, lockContent);

        foreach (var container in builder.Resources.OfType<ContainerResource>())
        {
            var lab = container.GetLabSettings();
            var containerName = container.Annotations.OfType<ContainerNameAnnotation>().LastOrDefault()?.Name ?? container.Name;

            // Explicit (standard protocol) ports, listed for the docs and the duplicate check.
            var generated = lab.LabPorts.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var endpoint in container.Annotations.OfType<EndpointAnnotation>().Where(e => e.Port is not null && !generated.Contains(e.Name)))
            {
                rows.Add(new PortRow("Protocol", endpoint.Port!.Value, endpoint.Protocol == System.Net.Sockets.ProtocolType.Udp ? "udp" : "tcp",
                    container.Name, containerName, endpoint.TargetPort ?? endpoint.Port.Value, endpoint.Name));
            }
            foreach (var (host, target, protocol) in lab.ExtraPorts)
            {
                rows.Add(new PortRow("Protocol", host, protocol, container.Name, containerName, target, "published"));
            }
        }

        var duplicates = rows.GroupBy(r => (r.HostPort, r.Protocol)).Where(g => g.Count() > 1)
            .Select(g => $"host port {g.Key.HostPort}/{g.Key.Protocol}: {string.Join(", ", g.Select(r => r.Resource))}")
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Host port collision (see docs/ports.md):\n  " + string.Join("\n  ", duplicates));
        }

        return rows;
    }

    /// <summary>docs/ports.md is generated from the same data that assigns the ports, so it cannot drift.</summary>
    private static void WritePortsDocument(IDistributedApplicationBuilder builder, List<PortRow> rows)
    {
        var text = new System.Text.StringBuilder()
            .AppendLine("# Host ports")
            .AppendLine()
            .AppendLine("<!-- Generated by the AppHost (src/RuntimeManager.cs, AssignHostPorts). Do not edit by hand. -->")
            .AppendLine()
            .AppendLine("Caddy reaches every service by `container_name:container port` on `flat_lab_net`; host ports are for LAN / debugging only.")
            .AppendLine("Web ports are `prefix * 1000 + position` in declaration order per homepage group; move a whole group with `PORT_PREFIX_<GROUP>` in `.env`.")
            .AppendLine("`10000-10099` belongs to `docker-compose.yml` (infra layer), `19000-19005` to the Aspire dashboard (`apphost.run.json`).")
            .AppendLine();

        foreach (var group in rows.GroupBy(r => r.Group).OrderBy(g => g.Min(r => r.Group == "Protocol" ? int.MaxValue : r.HostPort)))
        {
            text.AppendLine($"## {(group.Key == "Protocol" ? "Standard protocol ports" : group.Key)}")
                .AppendLine()
                .AppendLine("| Host port | Container | Container port | Resource / endpoint |")
                .AppendLine("|---|---|---|---|");
            foreach (var row in group.OrderBy(r => r.HostPort))
            {
                text.AppendLine($"| {row.HostPort}/{row.Protocol} | `{row.Container}` | {row.TargetPort} | {row.Resource} / {row.Endpoint} |");
            }
            text.AppendLine();
        }

        WriteIfChanged(Path.Combine(builder.AppHostDirectory, "docs", "ports.md"), text.ToString().Replace("\r\n", "\n"));
    }

    private static void WriteIfChanged(string path, string content)
    {
        try
        {
            if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != content)
            {
                File.WriteAllText(path, content);
            }
        }
        catch (IOException)
        {
            // Read-only checkout: ports are still assigned, only the generated file is stale.
        }
    }

    /// <summary>
    /// Host paths become <c>${FLAT_LAB_DATA:-…}</c> / <c>${FLAT_LAB_ROOT:-…}</c> expressions so the
    /// published compose file works on the server regardless of where it was generated.
    /// </summary>
    private static string ToComposePath(string hostPath, string publishDirectory, string repoRoot, string dataRoot)
    {
        // Unix system paths such as /var/run/docker.sock are not fully qualified on a Windows dev PC; keep them verbatim.
        if (!Path.IsPathFullyQualified(hostPath))
        {
            return hostPath;
        }

        static string Relative(string from, string to) => Path.GetRelativePath(from, to).Replace('\\', '/');

        if (IsUnder(hostPath, dataRoot))
        {
            var rest = Relative(dataRoot, hostPath);
            return $"${{FLAT_LAB_DATA:-{Relative(publishDirectory, dataRoot)}}}" + (rest == "." ? "" : "/" + rest);
        }

        // Everything else that sits on the same drive (repo files, siblings like ../snikket.conf) is repo-relative.
        if (Path.GetPathRoot(hostPath) == Path.GetPathRoot(repoRoot))
        {
            var rest = Relative(repoRoot, hostPath);
            return $"${{FLAT_LAB_ROOT:-{Relative(publishDirectory, repoRoot)}}}" + (rest == "." ? "" : "/" + rest);
        }

        return hostPath.Replace('\\', '/');
    }

    /// <summary>Escape '$' for compose interpolation, keeping Aspire-generated <c>${PLACEHOLDER}</c> references intact.</summary>
    private static string EscapeForCompose(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value, @"\$(?!\{[A-Za-z_][A-Za-z0-9_]*(:-[^}]*)?\})", "$$$$");

    private static bool IsUnder(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative == "." || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }
}
