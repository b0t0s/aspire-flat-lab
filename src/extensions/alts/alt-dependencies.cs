using static EnvironmentSettings;

namespace apphost.cs.src;

internal static class AltTechnicalServices
{
    public static DependenciesService AddAltDependencies(this DependenciesService dependencies)
    {
        var settings = dependencies.Settings;

        // Smarthome
        _ = dependencies.Postgres("gramps-db", "16-alpine", "gramps", "gramps", "GRAMPS_DB_PASSWORD",
            settings.SmartHome.DataPath("GRAMPS_DB_DATA", "../services-data/system/gramps-db"));
        _ = dependencies.Add("openemr-db", "mariadb", "11.8.8")
            .WithArgs("mariadbd", "--character-set-server=utf8mb4")
            .WithEnvironment("MYSQL_ROOT_PASSWORD", settings.Secret("OPENEMR_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_DATABASE", "openemr")
            .WithEnvironment("MYSQL_USER", "openemr")
            .WithEnvironment("MYSQL_PASSWORD", settings.Secret("OPENEMR_DB_PASS"))
            .WithBindMount(settings.SmartHome.DataPath("OPENEMR_DB_DATA", "../services-data/system/openemr-db"), "/var/lib/mysql")
            .WithHealthcheck("/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized", interval: "1m", retries: 3, startPeriod: "1m");
        var plausibleDbPassword = settings.Secret("PLAUSIBLE_DB_PASSWORD");
        var clickhousePassword = settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD");
        _ = dependencies.Postgres("plausible-db", "16-alpine", "plausible", "plausible", "PLAUSIBLE_DB_PASSWORD",
            settings.SmartHome.DataPath("PLAUSIBLE_DB_DATA", "../services-data/system/plausible/postgres"));
        _ = dependencies.Add("plausible-clickhouse", "clickhouse/clickhouse-server", "24.3-alpine")
            .WithEnvironment("CLICKHOUSE_DB", "plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
            .WithBindMount(settings.SmartHome.DataPath("PLAUSIBLE_CLICKHOUSE_DATA", "../services-data/system/plausible/clickhouse"), "/var/lib/clickhouse");
        _ = dependencies.Add("plausible-events", "plausible/events", "latest")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{plausibleDbPassword}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", clickhousePassword)
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("BASE_URL", $"https://analytics.{settings.Shared.DuckDnsDomain}")
            .WaitFor(dependencies["plausible-db"])
            .WaitFor(dependencies["plausible-clickhouse"]);

        // Development
        _ = dependencies.Postgres("kutt-db", "16-alpine", "kutt", "kutt", "KUTT_DB_PASSWORD",
            settings.Development.DataPath("KUTT_DB_DATA", "../services-data/system/kutt/postgres"));
        _ = dependencies.Redis("kutt-redis");

        // Knowledge
        _ = dependencies.Postgres("affine-db", "16-alpine", "affine", "affine", "AFFINE_DB_PASSWORD",
            settings.Knowledge.DataPath("AFFINE_DB_DATA", "../services-data/system/affine/postgres"));
        _ = dependencies.Postgres("karakeep-db", "16-alpine", "karakeep", "karakeep", "KARAKEEP_DB_PASSWORD",
            settings.Knowledge.DataPath("KARAKEEP_DB_DATA", "../services-data/system/karakeep/postgres"));
        _ = dependencies.Redis("karakeep-redis");
        _ = dependencies.Meilisearch("karakeep-meilisearch", "KARAKEEP_MEILI_KEY",
            settings.Knowledge.DataPath("KARAKEEP_MEILI_DATA", "../services-data/system/karakeep/meilisearch"));
        _ = dependencies.Postgres("linkwarden-db", "16-alpine", "linkwarden", "linkwarden", "LINKWARDEN_DB_PASSWORD",
            settings.Knowledge.DataPath("LINKWARDEN_DB_DATA", "../services-data/system/linkwarden/postgres"));
        _ = dependencies.Meilisearch("linkwarden-meilisearch", "LINKWARDEN_MEILI_KEY",
            settings.Knowledge.DataPath("LINKWARDEN_MEILI_DATA", "../services-data/system/linkwarden/meilisearch"));
        _ = dependencies.Postgres("outline-db", "16-alpine", "outline", "outline", "OUTLINE_DB_PASSWORD",
            settings.Knowledge.DataPath("OUTLINE_DB_DATA", "../services-data/system/outline/postgres"));
        _ = dependencies.Redis("outline-redis");
        _ = dependencies.MariaDb("bookstack-db", "11", "bookstack", "bookstack", "BOOKSTACK_DB_PASSWORD",
            settings.Knowledge.DataPath("BOOKSTACK_DB_DATA", "../services-data/system/bookstack/db"));
        _ = dependencies.Postgres("keila-db", "16-alpine", "keila", "keila", "KEILA_DB_PASSWORD",
            settings.Knowledge.DataPath("KEILA_DB_DATA", "../services-data/system/keila/postgres"));
        _ = dependencies.Postgres("joplin-db", "16-alpine", "joplin", "joplin", "JOPLIN_DB_PASSWORD",
            settings.Knowledge.DataPath("JOPLIN_DB_DATA", "../services-data/system/joplin/postgres"));

        _ = dependencies.Postgres("oxicloud-db", "16-alpine", "oxicloud", "oxicloud", "OXICLOUD_DB_PASSWORD",
            settings.SmartHome.DataPath("OXICLOUD_DB_DATA", "../services-data/system/oxicloud/postgres"));

        // Chat
        _ = dependencies.Postgres("synapse-db", "16-alpine", "synapse", "synapse", "SYNAPSE_DB_PASSWORD",
                settings.Chat.DataPath("SYNAPSE_DB_DATA", "../services-data/system/synapse/postgres"))
            .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8 --lc-collate=C --lc-ctype=C");
        _ = dependencies.Postgres("lemmy-db", "16-alpine", "lemmy", "lemmy", "LEMMY_DB_PASSWORD",
            settings.Chat.DataPath("LEMMY_DB_DATA", "../services-data/system/lemmy/postgres"));
        _ = dependencies.Add("lemmy-project-pictrs", "asonix/pictrs", "0.5.24")
            .WithContainerNetworkAlias("pictrs")
            .WithEnvironment("PICTRS__SERVER__API_KEY", settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"))
            .WithEnvironment("RUST_BACKTRACE", "full")
            .WithEnvironment("PICTRS__MEDIA__VIDEO__VIDEO_CODEC", "vp9")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_WIDTH", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_HEIGHT", "256")
            .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_FRAME_COUNT", "400")
            .WithBindMount(Path.Combine(settings.Chat.DataPath("LEMMY_PROJECT_DATA", "../services-data/system/lemmy-project"), "pictrs"), "/mnt")
            .WithLab(lab => lab.User = "991:991");

        return dependencies;
    }
}
