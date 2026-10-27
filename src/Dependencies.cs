namespace apphost.cs.src;

internal sealed class DependenciesService : Dictionary<string, IResourceBuilder<ContainerResource>>;

internal static class TechnicalServices
{
    private const string PgIsReady = "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB";
    
    public static DependenciesService AddDependencies(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        var resources = new DependenciesService();

        IResourceBuilder<ContainerResource> Add(string name, string image, string tag)
        {
            var container = builder.AddContainer(name, image, tag)
                .WithContainerName(name)
                .WithStartPolicy(settings.StartByDefaultForResource(name));
            resources.Add(name, container);
            return container;
        }

        IResourceBuilder<ContainerResource> Postgres(string name, string tag, string database, string user, string passwordKey, string dataPath,
            string dataTarget = "/var/lib/postgresql/data", params string[] passwordAliases)
        {
            return Add(name, "postgres", tag)
                .WithEnvironment("POSTGRES_DB", database)
                .WithEnvironment("POSTGRES_USER", user)
                .WithEnvironment("POSTGRES_PASSWORD", settings.Secret(passwordKey, aliases: passwordAliases))
                .WithBindMount(dataPath, dataTarget)
                .WithHealthcheck(PgIsReady);
        }

        IResourceBuilder<ContainerResource> Redis(string name, string? dataPath = null)
        {
            var redis = Add(name, "redis", "7-alpine").WithHealthcheck("redis-cli ping");
            return dataPath is null ? redis : redis.WithBindMount(dataPath, "/data");
        }

        // Entertainment
        _ = Postgres("miniflux-db", "16-alpine", "miniflux", "miniflux", "MINIFLUX_DB_PASSWORD",
            settings.Entertainment.DataPath("MINIFLUX_DB_DATA", "../services-data/system/miniflux/postgres"));
        _ = Add("rsshub", "diygod/rsshub", "latest")
            .WithEnvironment("NODE_ENV", "production");
        _ = Add("ollama", "ollama/ollama", "latest")
            .WithEnvironment("OLLAMA_ORIGINS", "*")
            .WithEnvironment("OLLAMA_HOST", "0.0.0.0")
            .WithEnvironment("OLLAMA_KEEP_ALIVE", "24h")
            .WithEnvironment("OLLAMA_API_KEY", settings.Chat.Value("OLLAMA_API_KEY"))
            .WithBindMount(settings.Chat.DataPath("OLLAMA_DATA", "../services-data/system/ollama"), "/root/.ollama")
            .WithPublishedPort(11434, 11434);

        // Smarthome

        // Development

        // Knowledge
        _ = Add("ghost-db", "mysql", "8.0")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("GHOST_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "ghost")
            .WithEnvironment("MYSQL_USER", "ghost")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("GHOST_DB_PASS"))
            .WithBindMount(settings.Knowledge.DataPath("GHOST_DB_DATA", "../services-data/system/ghost-db"), "/var/lib/mysql")
            .WithHealthcheck("mysqladmin ping -h 127.0.0.1 -uroot --password=\"$MYSQL_ROOT_PASSWORD\"");
        _ = Postgres("paperless-db", "16-alpine", "paperless", "paperless", "PAPERLESS_DB_PASSWORD",
            settings.Knowledge.DataPath("PAPERLESS_DB_DATA", "../services-data/system/paperless/postgres"));
        _ = Redis("paperless-redis", settings.Knowledge.DataPath("PAPERLESS_REDIS_DATA", "../services-data/system/paperless/redis"));
        _ = Postgres("keila-db", "16-alpine", "keila", "keila", "KEILA_DB_PASSWORD",
            settings.Knowledge.DataPath("KEILA_DB_DATA", "../services-data/system/keila/postgres"));

        // Secrets
        _ = Postgres("infisical-db", "16-alpine", "infisical", "infisical", "INFISICAL_DB_PASSWORD",
            settings.Secrets.DataPath("INFISICAL_DB_DATA", "../services-data/system/infisical/postgres"));
        _ = Redis("infisical-redis");

        return resources;
    }
}
