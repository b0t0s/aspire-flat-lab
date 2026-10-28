using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Knowledge" - host ports 12xxx (docs/ports.md).</summary>
internal static class KnowledgeServices
{
    public static void AddKnowledgeServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("paperless-ngx", "ghcr.io/paperless-ngx/paperless-ngx", "latest")
            .WithContainerName("documents")
            .WithStartPolicy(true)
            .WithEnvironment("PAPERLESS_REDIS", "redis://paperless-redis:6379")
            .WithEnvironment("PAPERLESS_DBENGINE", "postgresql")
            .WithEnvironment("PAPERLESS_DBHOST", "paperless-db")
            .WithEnvironment("PAPERLESS_DBNAME", "paperless")
            .WithEnvironment("PAPERLESS_DBUSER", "paperless")
            .WithEnvironment("PAPERLESS_DBPASS", settings.Secret("PAPERLESS_DB_PASSWORD"))
            .WithEnvironment("PAPERLESS_SECRET_KEY", settings.Secret("PAPERLESS_SECRET", SecretKind.LongPassword))
            .WithEnvironment("PAPERLESS_TIME_ZONE", settings.Shared.TimeZone)
            .WithEnvironment("PAPERLESS_URL", $"https://documents.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("USERMAP_UID", settings.Shared.Uid)
            .WithEnvironment("USERMAP_GID", settings.Shared.Gid)
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_DATA", "../services-data/system/paperless/data"), "/usr/src/paperless/data")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_MEDIA", "../services-data/system/paperless/media"), "/usr/src/paperless/media")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_EXPORT", "../services-data/system/paperless/export"), "/usr/src/paperless/export")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_CONSUME", "../services-data/system/paperless/consume"), "/usr/src/paperless/consume")
            .WithLabPort(8000, "paperless-ngx")
            .WaitFor(dependencies["paperless-redis"])
            .WaitFor(dependencies["paperless-db"])
            .WithHomepage("Knowledge", "Documents", "mdi-file-pdf-box", href: $"https://documents.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("paperlessngx", "http://documents:8000", key: HomepageVar("PAPERLESS_WIDGET_KEY"));


        _ = builder.AddContainer("faved", "denho/faved", "latest")
            .WithContainerName("bookmarks")
            .WithStartPolicy(true)
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("SECRET_KEY", settings.Secret("FAVED_SECRET"))
            .WithBindMount(settings.Knowledge.DataPath("FAVED_DATA", "../services-data/system/faved"), "/var/www/html/storage/")
            .WithLabPort(80, "faved")
            .WithHomepage("Knowledge", "Bookmarks", "si-pocket", href: $"https://bookmarks.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("memos", "neosmemo/memos", "stable")
            .WithContainerName("favourites")
            .WithStartPolicy(true)
            .WithBindMount(settings.Knowledge.DataPath("MEMOS_DATA", "../services-data/system/memos"), "/var/opt/memos")
            .WithLabPort(5230, "memos")
            .WithHomepage("Knowledge", "Favourites", "mdi-note-multiple", "Microblog", $"https://favourites.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ghost", "ghost", "5")
            .WithContainerName("blog")
            .WithStartPolicy(settings.Knowledge.StartByDefault)
            .WithEnvironment("database__client", "mysql")
            .WithEnvironment("database__connection__host", "ghost-db")
            .WithEnvironment("database__connection__user", "ghost")
            .WithEnvironment("database__connection__password", settings.Secret("GHOST_DB_PASS"))
            .WithEnvironment("database__connection__database", "ghost")
            .WithEnvironment("url", $"https://blog.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Knowledge.DataPath("GHOST_DATA", "../services-data/system/ghost"), "/var/lib/ghost/content")
            .WithLabPort(2368, "ghost")
            .WaitFor(dependencies["ghost-db"])
            .WithHomepage("Knowledge", "Blog", "si-ghost", "Blog and posts", $"https://blog.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("nostos", "ghcr.io/christian-gennari/nostos", "latest")
            .WithContainerName("nostos")
            .WithStartPolicy(settings.Knowledge.StartByDefault)
            .WithBindMount(settings.Knowledge.DataPath("NOSTOS_DATA", "../services-data/system/nostos"), "/data")
            .WithLabPort(8080, "nostos")
            .WithHomepage("Knowledge", "Nostos", "mdi-book-open-variant", "", $"https://nostos.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("vaultisse", "ghcr.io/albertamat/vaultisse", "latest")
            .WithContainerName("vaultisse")
            .WithStartPolicy(settings.Knowledge.StartByDefault)
            .WithEnvironment("DB_HOST", "vaultisse-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "vaultisse")
            .WithEnvironment("DB_USER", "vaultisse")
            .WithEnvironment("DB_PASSWORD", settings.Secret("VAULTISSE_DB_PASSWORD"))
            .WithBindMount(settings.Knowledge.DataPath("VAULTISSE_LOGS", "../services-data/system/vaultisse/logs"), "/app/logs")
            .WithLabPort(3000, "vaultisse")
            .WaitFor(dependencies["vaultisse-db"])
            .WithHomepage("Knowledge", "Book lending", "mdi-bookshelf", "", $"https://vaultisse.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("zensical", "zensical/zensical", "latest")
            .WithContainerName("zensical")
            .WithStartPolicy(settings.Knowledge.StartByDefault)
            .WithArgs("serve", "--dev-addr=0.0.0.0:8000")
            .WithBindMount(settings.Knowledge.DataPath("ZENSICAL_DOCS", "../services-data/public/docs2"), "/docs")
            .WithLabPort(8000, "zensical")
            .WithHomepage("Knowledge", "Zensical", "mdi-book-open-page-variant", "Docs server", $"https://docs2.{settings.Shared.DuckDnsDomain}");
    }
}
