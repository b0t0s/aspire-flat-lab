namespace apphost.cs.src;

internal sealed class DependenciesService(IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    : Dictionary<string, IResourceBuilder<ContainerResource>>
{
    private const string PgIsReady = "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB";
    private const string MariaDbReady = "/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized";

    public EnvironmentSettings Settings => settings;

    public IResourceBuilder<ContainerResource> Add(string name, string image, string tag)
    {
        var container = builder.AddContainer(name, image, tag).WithContainerName(name);
        Add(name, container);
        return container;
    }

    public IResourceBuilder<ContainerResource> Postgres(string name, string tag, string database, string user, string passwordKey, string dataPath,
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

    public IResourceBuilder<ContainerResource> MariaDb(string name, string tag, string database, string user, string passwordKey, string dataPath)
    {
        return Add(name, "mariadb", tag)
            .WithEnvironment("MARIADB_DATABASE", database)
            .WithEnvironment("MARIADB_USER", user)
            .WithEnvironment("MARIADB_PASSWORD", settings.Secret(passwordKey))
            .WithEnvironment("MARIADB_RANDOM_ROOT_PASSWORD", "true")
            .WithBindMount(dataPath, "/var/lib/mysql")
            .WithHealthcheck(MariaDbReady, startPeriod: "30s");
    }

    public IResourceBuilder<ContainerResource> Redis(string name, string? dataPath = null)
    {
        var redis = Add(name, "redis", "7-alpine").WithHealthcheck("redis-cli ping");
        return dataPath is null ? redis : redis.WithBindMount(dataPath, "/data");
    }

    public IResourceBuilder<ContainerResource> Meilisearch(string name, string masterKeyKey, string dataPath)
    {
        return Add(name, "getmeili/meilisearch", "v1.10")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret(masterKeyKey))
            .WithEnvironment("MEILI_ENV", "production")
            .WithBindMount(dataPath, "/meili_data");
    }
}

internal static class TechnicalServices
{
    public static DependenciesService AddDependencies(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        var dependencies = new DependenciesService(builder, settings);

        // Entertainment
        _ = dependencies.Postgres("miniflux-db", "16-alpine", "miniflux", "miniflux", "MINIFLUX_DB_PASSWORD",
            settings.Entertainment.DataPath("MINIFLUX_DB_DATA", "../services-data/system/miniflux/postgres"));
        _ = dependencies.Add("rsshub", "diygod/rsshub", "latest")
            .WithEnvironment("NODE_ENV", "production");

        // Chat
        _ = dependencies.Add("ollama", "ollama/ollama", "latest")
            .WithEnvironment("OLLAMA_ORIGINS", "*")
            .WithEnvironment("OLLAMA_HOST", "0.0.0.0")
            .WithEnvironment("OLLAMA_KEEP_ALIVE", "24h")
            .WithEnvironment("OLLAMA_API_KEY", settings.Chat.Value("OLLAMA_API_KEY"))
            .WithBindMount(settings.Chat.DataPath("OLLAMA_DATA", "../services-data/system/ollama"), "/root/.ollama")
            .WithPublishedPort(11434, 11434);

        // Development
        _ = dependencies.Postgres("atuin-db", "16-alpine", "atuin", "atuin", "ATUIN_DB_PASSWORD",
            settings.Development.DataPath("ATUIN_DB_DATA", "../services-data/system/atuin/postgres"));
        _ = dependencies.Postgres("ideon-db", "18-alpine", "ideon", "ideon", "IDEON_DB_PASSWORD",
            settings.Development.DataPath("IDEON_DB_DATA", "../services-data/system/ideon/postgres"));
        _ = dependencies.Postgres("usertour-db", "16-alpine", "usertour", "postgres", "USERTOUR_DB_PASSWORD",
            settings.Development.DataPath("USERTOUR_DB_DATA", "../services-data/system/usertour/postgres"));
        _ = dependencies.Redis("usertour-redis", settings.Development.DataPath("USERTOUR_REDIS_DATA", "../services-data/system/usertour/redis"));

        _ = dependencies.Postgres("nextcloud-db", "16-alpine", "nextcloud", "nextcloud", "NEXTCLOUD_DB_PASSWORD",
            settings.SmartHome.DataPath("NEXTCLOUD_DB_DATA", "../services-data/system/nextcloud/postgres"));
        _ = dependencies.Redis("nextcloud-redis");

        // Knowledge
        _ = dependencies.Postgres("vaultisse-db", "16-alpine", "vaultisse", "vaultisse", "VAULTISSE_DB_PASSWORD",
            settings.Knowledge.DataPath("VAULTISSE_DB_DATA", "../services-data/system/vaultisse/postgres"));
        _ = dependencies.Add("ghost-db", "mysql", "8.0")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("GHOST_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "ghost")
            .WithEnvironment("MYSQL_USER", "ghost")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("GHOST_DB_PASS"))
            .WithBindMount(settings.Knowledge.DataPath("GHOST_DB_DATA", "../services-data/system/ghost-db"), "/var/lib/mysql")
            .WithHealthcheck("mysqladmin ping -h 127.0.0.1 -uroot --password=\"$MYSQL_ROOT_PASSWORD\"");
        _ = dependencies.Postgres("paperless-db", "16-alpine", "paperless", "paperless", "PAPERLESS_DB_PASSWORD",
            settings.Knowledge.DataPath("PAPERLESS_DB_DATA", "../services-data/system/paperless/postgres"));
        _ = dependencies.Redis("paperless-redis", settings.Knowledge.DataPath("PAPERLESS_REDIS_DATA", "../services-data/system/paperless/redis"));

        // Secrets
        _ = dependencies.Postgres("infisical-db", "16-alpine", "infisical", "infisical", "INFISICAL_DB_PASSWORD",
            settings.Secrets.DataPath("INFISICAL_DB_DATA", "../services-data/system/infisical/postgres"));
        _ = dependencies.Redis("infisical-redis");
        _ = dependencies.MariaDb("passbolt-db", "11", "passbolt", "passbolt", "PASSBOLT_DB_PASSWORD",
            settings.Secrets.DataPath("PASSBOLT_DB_DATA", "../services-data/system/passbolt/db"));
        _ = dependencies.MariaDb("psono-db", "11", "psono", "psono", "PSONO_DB_PASSWORD",
            settings.Secrets.DataPath("PSONO_DB_DATA", "../services-data/system/psono/db"));
        _ = dependencies.Postgres("documenso-db", "16-alpine", "documenso", "documenso", "DOCUMENSO_DB_PASSWORD",
            settings.Secrets.DataPath("DOCUMENSO_DB_DATA", "../services-data/system/documenso/postgres"));
        _ = dependencies.Redis("documenso-redis");

        // Media
        _ = dependencies.Postgres("ryot-db", "18-alpine", "postgres", "postgres", "RYOT_DB_PASSWORD",
            settings.Media.DataPath("RYOT_DB_DATA", "../services-data/system/ryot/postgres"), "/var/lib/postgresql");
        _ = dependencies.Postgres("piped-db", "16-alpine", "piped", "piped", "PIPED_DB_PASSWORD",
            settings.Media.DataPath("PIPED_DB_DATA", "../services-data/system/piped/postgres"));

        // Irl
        _ = dependencies.Postgres("medikeep-db", "16-alpine", "medical_records", "medapp", "MEDIKEEP_DB_PASSWORD",
            settings.Irl.DataPath("MEDIKEEP_DB_DATA", "../services-data/system/medikeep/postgres"));
        _ = dependencies.Postgres("healthlog-db", "16-alpine", "healthlog", "healthlog", "HEALTHLOG_DB_PASSWORD",
            settings.Irl.DataPath("HEALTHLOG_DB_DATA", "../services-data/system/healthlog/postgres"));
        _ = dependencies.Postgres("tradetally-db", "16-alpine", "tradetally", "trader", "TRADETALLY_DB_PASSWORD",
            settings.Irl.DataPath("TRADETALLY_DB_DATA", "../services-data/system/tradetally/postgres"));

        return dependencies;
    }
}
