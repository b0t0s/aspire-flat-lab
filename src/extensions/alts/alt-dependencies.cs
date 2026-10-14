using static EnvironmentSettings;

namespace apphost.cs.src;

internal sealed class DependenciesService : Dictionary<string, IResourceBuilder<ContainerResource>>;

internal static class TechnicalServices
{
    private const string PgIsReady = "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB";
    private const string MariaDbReady = "/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized";

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

        IResourceBuilder<ContainerResource> MariaDb(string name, string tag, string database, string user, string passwordKey, string dataPath)
        {
            return Add(name, "mariadb", tag)
                .WithEnvironment("MARIADB_DATABASE", database)
                .WithEnvironment("MARIADB_USER", user)
                .WithEnvironment("MARIADB_PASSWORD", settings.Secret(passwordKey))
                .WithEnvironment("MARIADB_RANDOM_ROOT_PASSWORD", "true")
                .WithBindMount(dataPath, "/var/lib/mysql")
                .WithHealthcheck(MariaDbReady, startPeriod: "30s");
        }

        IResourceBuilder<ContainerResource> Redis(string name, string? dataPath = null)
        {
            var redis = Add(name, "redis", "7-alpine").WithHealthcheck("redis-cli ping");
            return dataPath is null ? redis : redis.WithBindMount(dataPath, "/data");
        }

        IResourceBuilder<ContainerResource> Meilisearch(string name, string masterKeyKey, string dataPath)
        {
            return Add(name, "getmeili/meilisearch", "v1.10")
                .WithEnvironment("MEILI_MASTER_KEY", settings.Secret(masterKeyKey))
                .WithEnvironment("MEILI_ENV", "production")
                .WithBindMount(dataPath, "/meili_data");
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
        _ = Postgres("gramps-db", "16-alpine", "gramps", "gramps", "GRAMPS_DB_PASSWORD",
            settings.SmartHome.DataPath("GRAMPS_DB_DATA", "../services-data/system/gramps-db"));
        _ = Add("openemr-db", "mariadb", "11.8.8")
            .WithArgs("mariadbd", "--character-set-server=utf8mb4")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("OPENEMR_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "openemr")
            .WithEnvironment("MYSQL_USER", "openemr")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("OPENEMR_DB_PASS"))
            .WithBindMount(settings.SmartHome.DataPath("OPENEMR_DB_DATA", "../services-data/system/openemr-db"), "/var/lib/mysql")
            .WithHealthcheck(MariaDbReady, interval: "1m", retries: 3, startPeriod: "1m");
        var plausibleDbPassword = settings.Secret("PLAUSIBLE_DB_PASSWORD");
        var clickhousePassword = settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD");
        _ = Postgres("plausible-db", "16-alpine", "plausible", "plausible", "PLAUSIBLE_DB_PASSWORD",
            settings.SmartHome.DataPath("PLAUSIBLE_DB_DATA", "../services-data/system/plausible/postgres"));
        _ = Add("plausible-clickhouse", "clickhouse/clickhouse-server", "24.3-alpine")
            .WithEnvironment("CLICKHOUSE_DB", "plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
            .WithBindMount(settings.SmartHome.DataPath("PLAUSIBLE_CLICKHOUSE_DATA", "../services-data/system/plausible/clickhouse"), "/var/lib/clickhouse");
        _ = Add("plausible-events", "plausible/events", "latest")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{plausibleDbPassword}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("BASE_URL", $"https://analytics.{settings.Shared.DuckDnsDomain}")
            .WaitFor(resources["plausible-db"])
            .WaitFor(resources["plausible-clickhouse"]);

        _ = Add("beszel-agent", "henrygd/beszel-agent", "latest")
            .WithEnvironment("PORT", "45876")
            .WithEnvironment("KEY", settings.Secret("BESZEL_AGENT_KEY", SecretKind.Manual))
            .WithDockerSocket()
            .WithHostNetwork();

        // Development
        _ = Postgres("kutt-db", "16-alpine", "kutt", "kutt", "KUTT_DB_PASSWORD",
            settings.Development.DataPath("KUTT_DB_DATA", "../services-data/system/kutt/postgres"));
        _ = Redis("kutt-redis");

        // Knowledge
        _ = Add("ghost-db", "mysql", "8.0")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("GHOST_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "ghost")
            .WithEnvironment("MYSQL_USER", "ghost")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("GHOST_DB_PASS"))
            .WithBindMount(settings.Knowledge.DataPath("GHOST_DB_DATA", "../services-data/system/ghost-db"), "/var/lib/mysql")
            .WithHealthcheck("mysqladmin ping -h 127.0.0.1 -uroot --password=\"$MYSQL_ROOT_PASSWORD\"");
        _ = Postgres("affine-db", "16-alpine", "affine", "affine", "AFFINE_DB_PASSWORD",
            settings.Knowledge.DataPath("AFFINE_DB_DATA", "../services-data/system/affine/postgres"));
        _ = Postgres("karakeep-db", "16-alpine", "karakeep", "karakeep", "KARAKEEP_DB_PASSWORD",
            settings.Knowledge.DataPath("KARAKEEP_DB_DATA", "../services-data/system/karakeep/postgres"));
        _ = Redis("karakeep-redis");
        _ = Meilisearch("karakeep-meilisearch", "KARAKEEP_MEILI_KEY",
            settings.Knowledge.DataPath("KARAKEEP_MEILI_DATA", "../services-data/system/karakeep/meilisearch"));
        _ = Postgres("linkwarden-db", "16-alpine", "linkwarden", "linkwarden", "LINKWARDEN_DB_PASSWORD",
            settings.Knowledge.DataPath("LINKWARDEN_DB_DATA", "../services-data/system/linkwarden/postgres"));
        _ = Meilisearch("linkwarden-meilisearch", "LINKWARDEN_MEILI_KEY",
            settings.Knowledge.DataPath("LINKWARDEN_MEILI_DATA", "../services-data/system/linkwarden/meilisearch"));
        _ = Postgres("paperless-db", "16-alpine", "paperless", "paperless", "PAPERLESS_DB_PASSWORD",
            settings.Knowledge.DataPath("PAPERLESS_DB_DATA", "../services-data/system/paperless/postgres"));
        _ = Redis("paperless-redis", settings.Knowledge.DataPath("PAPERLESS_REDIS_DATA", "../services-data/system/paperless/redis"));
        _ = Postgres("outline-db", "16-alpine", "outline", "outline", "OUTLINE_DB_PASSWORD",
            settings.Knowledge.DataPath("OUTLINE_DB_DATA", "../services-data/system/outline/postgres"));
        _ = Redis("outline-redis");
        _ = MariaDb("bookstack-db", "11", "bookstack", "bookstack", "BOOKSTACK_DB_PASSWORD",
            settings.Knowledge.DataPath("BOOKSTACK_DB_DATA", "../services-data/system/bookstack/db"));
        _ = Postgres("keila-db", "16-alpine", "keila", "keila", "KEILA_DB_PASSWORD",
            settings.Knowledge.DataPath("KEILA_DB_DATA", "../services-data/system/keila/postgres"));

        // Media
        _ = Postgres("ryot-db", "18-alpine", "postgres", "postgres", "RYOT_DB_PASSWORD",
            settings.Media.DataPath("RYOT_DB_DATA", "../services-data/system/ryot/postgres"), "/var/lib/postgresql");
        _ = Redis("immich-redis", settings.Media.DataPath("IMMICH_REDIS_DATA", "../services-data/system/immich/redis"));
        _ = Add("immich-postgres", "ghcr.io/immich-app/postgres", "16-vectorchord0.5.3-pgvector0.8.1")
            .WithEnvironment("POSTGRES_USER", "immich")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("IMMICH_DB_PASSWORD"))
            .WithEnvironment("POSTGRES_DB", "immich")
            .WithBindMount(settings.Media.DataPath("IMMICH_PG_DATA", "../services-data/system/immich/postgres"), "/var/lib/postgresql/data")
            .WithHealthcheck(PgIsReady);
        _ = Add("immich-typesense", "typesense/typesense", "27.1")
            .WithEnvironment("TYPESENSE_API_KEY", settings.Secret("IMMICH_TYPESENSE_KEY"))
            .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
            .WithBindMount(settings.Media.DataPath("IMMICH_TYPESENSE_DATA", "../services-data/system/immich/typesense"), "/data");

        // Secrets
        _ = MariaDb("passbolt-db", "11", "passbolt", "passbolt", "PASSBOLT_DB_PASSWORD",
            settings.Secrets.DataPath("PASSBOLT_DB_DATA", "../services-data/system/passbolt/db"));
        _ = MariaDb("psono-db", "11", "psono", "psono", "PSONO_DB_PASSWORD",
            settings.Secrets.DataPath("PSONO_DB_DATA", "../services-data/system/psono/db"));
        _ = Postgres("infisical-db", "16-alpine", "infisical", "infisical", "INFISICAL_DB_PASSWORD",
            settings.Secrets.DataPath("INFISICAL_DB_DATA", "../services-data/system/infisical/postgres"));
        _ = Redis("infisical-redis");
        _ = Postgres("documenso-db", "16-alpine", "documenso", "documenso", "DOCUMENSO_DB_PASSWORD",
            settings.Secrets.DataPath("DOCUMENSO_DB_DATA", "../services-data/system/documenso/postgres"));
        _ = Redis("documenso-redis");

        // Chat
        _ = Postgres("synapse-db", "16-alpine", "synapse", "synapse", "SYNAPSE_DB_PASSWORD",
                settings.Chat.DataPath("SYNAPSE_DB_DATA", "../services-data/system/synapse/postgres"))
            .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8 --lc-collate=C --lc-ctype=C");
        _ = Postgres("lemmy-db", "16-alpine", "lemmy", "lemmy", "LEMMY_DB_PASSWORD",
            settings.Chat.DataPath("LEMMY_DB_DATA", "../services-data/system/lemmy/postgres"));
        _ = Redis("livekit-redis");

        // Snikket (XMPP all-in-one) needs host networking for its own 80/443/5222/5269.
        var snikketDataPath = settings.Chat.DataPath("SNIKKET_DATA_PATH", "../services-data/system/snikket");
        var snikketAcmePath = Path.Combine(snikketDataPath, "acme-challenge");
        IResourceBuilder<ContainerResource> Snikket(string name, string image)
        {
            return Add(name, image, "dev")
                .WithStartPolicy(settings.Chat.StartByDefault)
                .WithHostNetwork()
                .WithSnikketSettings(settings)
                .WithBindMount(snikketDataPath, "/snikket");
        }

        _ = Snikket("snikket-server", "snikket/snikket-server");
        _ = Snikket("snikket-proxy", "snikket/snikket-web-proxy")
            .WithBindMount(snikketAcmePath, "/var/www/html/.well-known/acme-challenge");
        _ = Snikket("snikket-certs", "snikket/snikket-cert-manager")
            .WithBindMount(snikketAcmePath, "/var/www/.well-known/acme-challenge");

        // Lemmy support containers (hostnames match configs/lemmy/nginx_internal.conf and lemmy.hjson).
        var lemmyVolumes = settings.Chat.DataPath("LEMMY_PROJECT_DATA", "../services-data/system/lemmy-project");
        _ = Add("lemmy-project-pictrs", "asonix/pictrs", "0.5.24")
            .WithContainerNetworkAlias("pictrs")
            .WithEnvironment("PICTRS__SERVER__API_KEY", settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"))
            .WithEnvironment("RUST_BACKTRACE", "full")
            .WithEnvironment("PICTRS__MEDIA__VIDEO__VIDEO_CODEC", "vp9")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_WIDTH", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_HEIGHT", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_FRAME_COUNT", "400")
            .WithBindMount(Path.Combine(lemmyVolumes, "pictrs"), "/mnt")
            .WithLab(lab => lab.User = "991:991");
        _ = Add("lemmy-project-postfix", "mwader/postfix-relay", "latest")
            .WithEnvironment("POSTFIX_myhostname", settings.Shared.DuckDnsDomain);
        _ = Add("lemmy-project-proxy", "nginx", "1-alpine")
            .WithContainerNetworkAlias("proxy")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithRepoMount("configs/lemmy/nginx_internal.conf", "/etc/nginx/nginx.conf", isReadOnly: true)
            .WithRepoMount("configs/lemmy/proxy_params", "/etc/nginx/proxy_params", isReadOnly: true)
            .WithLabPort(8536, "lemmy-project-proxy")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WithRestart("always");

        return resources;
    }
}
