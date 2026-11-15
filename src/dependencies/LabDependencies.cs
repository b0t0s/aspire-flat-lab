using System.Runtime.CompilerServices;

namespace apphost.cs.src;

// Databases and caches the services wait for. Each one is a property in src/dependencies/<group>-dependencies.cs,
// created on first use, so a service writes `.WaitFor(dependencies.NextcloudDb)` and an unused one is never registered.
internal sealed partial class LabDependencies(IDistributedApplicationBuilder builder, EnvironmentSettings settings)
{
    private readonly Dictionary<string, IResourceBuilder<ContainerResource>> created = new(StringComparer.Ordinal);

    private IDistributedApplicationBuilder Builder => builder;

    private EnvironmentSettings Settings => settings;

    private IResourceBuilder<ContainerResource> Once(Func<IResourceBuilder<ContainerResource>> create, [CallerMemberName] string property = "")
    {
        if (!created.TryGetValue(property, out var dependency))
        {
            dependency = create();
            created[property] = dependency;
        }
        return dependency;
    }

    private const string PgIsReady = "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB";
    private const string MariaDbReady = "/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized";

    private IResourceBuilder<ContainerResource> Add(string name, string image, string tag)
    {
        return builder.AddContainer(name, image, tag).WithContainerName(name);
    }

    private IResourceBuilder<ContainerResource> Postgres(string name, string tag, string database, string user, string passwordKey, string dataPath,
        string dataTarget = "/var/lib/postgresql/data", params string[] passwordAliases)
    {
        var container = Add(name, "postgres", tag)
            .WithEnvironment("POSTGRES_DB", database)
            .WithEnvironment("POSTGRES_USER", user)
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret(passwordKey, aliases: passwordAliases))
            .WithBindMount(dataPath, dataTarget)
            .WithHealthcheck(PgIsReady);
        if (int.TryParse(tag.Split('-')[0], out var major) && major >= 18)
        {
            container = container.WithEnvironment("PGDATA", dataTarget);
        }
        return container;
    }

    private IResourceBuilder<ContainerResource> MariaDb(string name, string tag, string database, string user, string passwordKey, string dataPath)
    {
        return Add(name, "mariadb", tag)
            .WithEnvironment("MARIADB_DATABASE", database)
            .WithEnvironment("MARIADB_USER", user)
            .WithEnvironment("MARIADB_PASSWORD", settings.Secret(passwordKey))
            .WithEnvironment("MARIADB_RANDOM_ROOT_PASSWORD", "true")
            .WithBindMount(dataPath, "/var/lib/mysql")
            .WithHealthcheck(MariaDbReady, startPeriod: "30s");
    }

    private IResourceBuilder<ContainerResource> Redis(string name, string? dataPath = null)
    {
        var redis = Add(name, "redis", "7-alpine").WithHealthcheck("redis-cli ping");
        return dataPath is null ? redis : redis.WithBindMount(dataPath, "/data");
    }

    private IResourceBuilder<ContainerResource> Meilisearch(string name, string masterKeyKey, string dataPath)
    {
        return Add(name, "getmeili/meilisearch", "v1.10")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret(masterKeyKey))
            .WithEnvironment("MEILI_ENV", "production")
            .WithBindMount(dataPath, "/meili_data");
    }
}
