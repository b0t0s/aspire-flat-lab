using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

public sealed class DependenciesService : Dictionary<string, IResourceBuilder<ContainerResource>>;

/// <summary>
/// Databases and caches consumed by the app services. Each database password is a secret parameter
/// shared by the database container and the app's connection string.
/// Dependencies of infra services (seafile, searxng) live next to them in docker-compose.yml.
/// </summary>
public static class TechnicalServices
{
    private const string PgIsReady = "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB";
    private const string MariaDbReady = "/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized";

    public static DependenciesService AddDependencies(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var dev = settings.Development;
        var tools = settings.Tools;
        var monitor = settings.Monitor;
        var irl = settings.Irl;
        var secrets = settings.Secrets;
        var media = settings.Media;
        var productivity = settings.Productivity;
        var knowledge = settings.Knowledge;
        var chat = settings.Chat;
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
            string dataTarget = "/var/lib/postgresql/data", params string[] passwordAliases) =>
            Add(name, "postgres", tag)
                .WithEnvironment("POSTGRES_DB", database)
                .WithEnvironment("POSTGRES_USER", user)
                .WithEnvironment("POSTGRES_PASSWORD", settings.Secret(passwordKey, aliases: passwordAliases))
                .WithBindMount(dataPath, dataTarget)
                .WithHealthcheck(PgIsReady);

        IResourceBuilder<ContainerResource> MariaDb(string name, string tag, string database, string user, string passwordKey, string dataPath) =>
            Add(name, "mariadb", tag)
                .WithEnvironment("MARIADB_DATABASE", database)
                .WithEnvironment("MARIADB_USER", user)
                .WithEnvironment("MARIADB_PASSWORD", settings.Secret(passwordKey))
                .WithEnvironment("MARIADB_RANDOM_ROOT_PASSWORD", "true")
                .WithBindMount(dataPath, "/var/lib/mysql")
                .WithHealthcheck(MariaDbReady, startPeriod: "30s");

        IResourceBuilder<ContainerResource> Redis(string name, string? dataPath = null)
        {
            var redis = Add(name, "redis", "7-alpine").WithHealthcheck("redis-cli ping");
            return dataPath is null ? redis : redis.WithBindMount(dataPath, "/data");
        }

        IResourceBuilder<ContainerResource> Meilisearch(string name, string masterKeyKey, string dataPath) =>
            Add(name, "getmeili/meilisearch", "v1.10")
                .WithEnvironment("MEILI_MASTER_KEY", settings.Secret(masterKeyKey))
                .WithEnvironment("MEILI_ENV", "production")
                .WithBindMount(dataPath, "/meili_data");

        // Tools
        Postgres("ryot-db", "18-alpine", "postgres", "postgres", "RYOT_DB_PASSWORD",
            tools.DataPath("RYOT_DB_DATA", "../services-data/system/ryot/postgres"), "/var/lib/postgresql");
        Postgres("kutt-db", "16-alpine", "kutt", "kutt", "KUTT_DB_PASSWORD",
            tools.DataPath("KUTT_DB_DATA", "../services-data/system/kutt/postgres"));
        Redis("kutt-redis");

        // Monitor
        var plausibleDbPassword = settings.Secret("PLAUSIBLE_DB_PASSWORD");
        var clickhousePassword = settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD");
        Postgres("plausible-db", "16-alpine", "plausible", "plausible", "PLAUSIBLE_DB_PASSWORD",
            monitor.DataPath("PLAUSIBLE_DB_DATA", "../services-data/system/plausible/postgres"));
        Add("plausible-clickhouse", "clickhouse/clickhouse-server", "24.3-alpine")
            .WithEnvironment("CLICKHOUSE_DB", "plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
            .WithBindMount(monitor.DataPath("PLAUSIBLE_CLICKHOUSE_DATA", "../services-data/system/plausible/clickhouse"), "/var/lib/clickhouse");
        Add("plausible-events", "plausible/events", "latest")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{plausibleDbPassword}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("BASE_URL", $"https://analytics.{shared.DuckDnsDomain}")
            .WaitFor(resources["plausible-db"])
            .WaitFor(resources["plausible-clickhouse"]);
        // The agent key is issued by the Beszel hub UI ("Add system").
        Add("beszel-agent", "henrygd/beszel-agent", "latest")
            .WithEnvironment("PORT", "45876")
            .WithEnvironment("KEY", settings.Secret("BESZEL_AGENT_KEY", SecretKind.Manual))
            .WithDockerSocket()
            .WithHostNetwork();

        // IRL
        Postgres("gramps-db", "16-alpine", "gramps", "gramps", "GRAMPS_DB_PASSWORD",
            irl.DataPath("GRAMPS_DB_DATA", "../services-data/system/gramps-db"));
        Add("openemr-db", "mariadb", "11.8.8")
            .WithArgs("mariadbd", "--character-set-server=utf8mb4")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("OPENEMR_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "openemr")
            .WithEnvironment("MYSQL_USER", "openemr")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("OPENEMR_DB_PASS"))
            .WithBindMount(irl.DataPath("OPENEMR_DB_DATA", "../services-data/system/openemr-db"), "/var/lib/mysql")
            .WithHealthcheck(MariaDbReady, interval: "1m", retries: 3, startPeriod: "1m");

        // Knowledge
        Add("ghost-db", "mysql", "8.0")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("GHOST_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "ghost")
            .WithEnvironment("MYSQL_USER", "ghost")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("GHOST_DB_PASS"))
            .WithBindMount(knowledge.DataPath("GHOST_DB_DATA", "../services-data/system/ghost-db"), "/var/lib/mysql")
            .WithHealthcheck("mysqladmin ping -h 127.0.0.1 -uroot --password=\"$MYSQL_ROOT_PASSWORD\"");
        Postgres("karakeep-db", "16-alpine", "karakeep", "karakeep", "KARAKEEP_DB_PASSWORD",
            knowledge.DataPath("KARAKEEP_DB_DATA", "../services-data/system/karakeep/postgres"));
        Redis("karakeep-redis");
        Meilisearch("karakeep-meilisearch", "KARAKEEP_MEILI_KEY",
            knowledge.DataPath("KARAKEEP_MEILI_DATA", "../services-data/system/karakeep/meilisearch"));
        Postgres("linkwarden-db", "16-alpine", "linkwarden", "linkwarden", "LINKWARDEN_DB_PASSWORD",
            knowledge.DataPath("LINKWARDEN_DB_DATA", "../services-data/system/linkwarden/postgres"));
        Meilisearch("linkwarden-meilisearch", "LINKWARDEN_MEILI_KEY",
            knowledge.DataPath("LINKWARDEN_MEILI_DATA", "../services-data/system/linkwarden/meilisearch"));
        Postgres("paperless-db", "16-alpine", "paperless", "paperless", "PAPERLESS_DB_PASSWORD",
            knowledge.DataPath("PAPERLESS_DB_DATA", "../services-data/system/paperless/postgres"));
        Redis("paperless-redis", knowledge.DataPath("PAPERLESS_REDIS_DATA", "../services-data/system/paperless/redis"));
        Postgres("outline-db", "16-alpine", "outline", "outline", "OUTLINE_DB_PASSWORD",
            knowledge.DataPath("OUTLINE_DB_DATA", "../services-data/system/outline/postgres"));
        Redis("outline-redis");
        MariaDb("bookstack-db", "11", "bookstack", "bookstack", "BOOKSTACK_DB_PASSWORD",
            knowledge.DataPath("BOOKSTACK_DB_DATA", "../services-data/system/bookstack/db"));
        Postgres("keila-db", "16-alpine", "keila", "keila", "KEILA_DB_PASSWORD",
            knowledge.DataPath("KEILA_DB_DATA", "../services-data/system/keila/postgres"));

        // Media
        Redis("immich-redis", media.DataPath("IMMICH_REDIS_DATA", "../services-data/system/immich/redis"));
        Add("immich-postgres", "ghcr.io/immich-app/postgres", "16-vectorchord0.5.3-pgvector0.8.1")
            .WithEnvironment("POSTGRES_USER", "immich")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("IMMICH_DB_PASSWORD"))
            .WithEnvironment("POSTGRES_DB", "immich")
            .WithBindMount(media.DataPath("IMMICH_PG_DATA", "../services-data/system/immich/postgres"), "/var/lib/postgresql/data")
            .WithHealthcheck(PgIsReady);
        Add("immich-typesense", "typesense/typesense", "27.1")
            .WithEnvironment("TYPESENSE_API_KEY", settings.Secret("IMMICH_TYPESENSE_KEY"))
            .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
            .WithBindMount(media.DataPath("IMMICH_TYPESENSE_DATA", "../services-data/system/immich/typesense"), "/data");

        // Productivity
        Postgres("affine-db", "16-alpine", "affine", "affine", "AFFINE_DB_PASSWORD",
            productivity.DataPath("AFFINE_DB_DATA", "../services-data/system/affine/postgres"));
        Postgres("miniflux-db", "16-alpine", "miniflux", "miniflux", "MINIFLUX_DB_PASSWORD",
            productivity.DataPath("MINIFLUX_DB_DATA", "../services-data/system/miniflux/postgres"));
        Postgres("documenso-db", "16-alpine", "documenso", "documenso", "DOCUMENSO_DB_PASSWORD",
            productivity.DataPath("DOCUMENSO_DB_DATA", "../services-data/system/documenso/postgres"));
        Redis("documenso-redis");

        // Secrets
        MariaDb("passbolt-db", "11", "passbolt", "passbolt", "PASSBOLT_DB_PASSWORD",
            secrets.DataPath("PASSBOLT_DB_DATA", "../services-data/system/passbolt/db"));
        MariaDb("psono-db", "11", "psono", "psono", "PSONO_DB_PASSWORD",
            secrets.DataPath("PSONO_DB_DATA", "../services-data/system/psono/db"));
        Postgres("infisical-db", "16-alpine", "infisical", "infisical", "INFISICAL_DB_PASSWORD",
            secrets.DataPath("INFISICAL_DB_DATA", "../services-data/system/infisical/postgres"));
        Redis("infisical-redis");

        // Chat
        Postgres("synapse-db", "16-alpine", "synapse", "synapse", "SYNAPSE_DB_PASSWORD",
                chat.DataPath("SYNAPSE_DB_DATA", "../services-data/system/synapse/postgres"))
            .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8 --lc-collate=C --lc-ctype=C");
        Postgres("lemmy-db", "16-alpine", "lemmy", "lemmy", "LEMMY_DB_PASSWORD",
            chat.DataPath("LEMMY_DB_DATA", "../services-data/system/lemmy/postgres"));
        Redis("livekit-redis");

        // Snikket (XMPP all-in-one) needs host networking for its own 80/443/5222/5269.
        var snikketDataPath = chat.DataPath("SNIKKET_DATA_PATH", "../services-data/system/snikket");
        var snikketAcmePath = Path.Combine(snikketDataPath, "acme-challenge");
        IResourceBuilder<ContainerResource> Snikket(string name, string image) =>
            Add(name, image, "dev")
                .WithStartPolicy(settings.ChatVariants.StartByDefault)
                .WithHostNetwork()
                .WithSnikketSettings(settings)
                .WithBindMount(snikketDataPath, "/snikket");
        Snikket("snikket-server", "snikket/snikket-server");
        Snikket("snikket-proxy", "snikket/snikket-web-proxy")
            .WithBindMount(snikketAcmePath, "/var/www/html/.well-known/acme-challenge");
        Snikket("snikket-certs", "snikket/snikket-cert-manager")
            .WithBindMount(snikketAcmePath, "/var/www/.well-known/acme-challenge");

        // Lemmy support containers (hostnames match configs/lemmy/nginx_internal.conf and lemmy.hjson).
        var lemmyVolumes = chat.DataPath("LEMMY_PROJECT_DATA", "../services-data/system/lemmy-project");
        Add("lemmy-project-pictrs", "asonix/pictrs", "0.5.24")
            .WithContainerNetworkAlias("pictrs")
            .WithEnvironment("PICTRS__SERVER__API_KEY", settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"))
            .WithEnvironment("RUST_BACKTRACE", "full")
            .WithEnvironment("PICTRS__MEDIA__VIDEO__VIDEO_CODEC", "vp9")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_WIDTH", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_HEIGHT", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_FRAME_COUNT", "400")
            .WithBindMount(Path.Combine(lemmyVolumes, "pictrs"), "/mnt")
            .WithLab(lab => lab.User = "991:991");
        Add("lemmy-project-postfix", "mwader/postfix-relay", "latest")
            .WithEnvironment("POSTFIX_myhostname", shared.DuckDnsDomain);
        Add("lemmy-project-proxy", "nginx", "1-alpine")
            .WithContainerNetworkAlias("proxy")
            .WithStartPolicy(chat.StartByDefault)
            .WithRepoMount("configs/lemmy/nginx_internal.conf", "/etc/nginx/nginx.conf", isReadOnly: true)
            .WithRepoMount("configs/lemmy/proxy_params", "/etc/nginx/proxy_params", isReadOnly: true)
            .WithLabPort(8536, "lemmy-project-proxy")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WithRestart("always");

        return resources;
    }
}
