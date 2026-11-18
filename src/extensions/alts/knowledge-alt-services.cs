using apphost.cs.src;
using static EnvironmentSettings;

internal static class KnowledgeAltServices
{
    public static void AddKnowledgeAltServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("calibre-web", "lscr.io/linuxserver/calibre-web", "latest")
            .WithContainerName("archive")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Knowledge.DataPath("CALIBRE_DATA", "../services-data/system/calibre-web"), "/config")
            .WithBindMount(settings.Knowledge.DataPath("CALIBRE_LIBRARY", "../services-data/public/books"), "/books")
            .WithLabPort(8083, "calibre-web")
            .WithHomepage("Knowledge", "Archive", "mdi-book-open-page-variant", "Ebook library", href: $"https://archive.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("calibreweb", "http://archive:8083",
                username: HomepageVar("CALIBRE_WIDGET_USER"),
                password: HomepageVar("CALIBRE_WIDGET_PASSWORD"));

        _ = builder.AddContainer("archivebox", "archivebox/archivebox", "latest")
            .WithContainerName("history")
            .WithEnvironment("ALLOWED_HOSTS", "*")
            .WithEnvironment("ADMIN_USERNAME", settings.Knowledge.Value("ARCHIVEBOX_ADMIN_USER"))
            .WithEnvironment("ADMIN_PASSWORD", settings.Secret("ARCHIVEBOX_ADMIN_PASS"))
            .WithBindMount(settings.Knowledge.DataPath("ARCHIVEBOX_DATA", "../services-data/system/archivebox"), "/data")
            .WithLabPort(8000, "archivebox")
            .WithHomepage("Knowledge", "History", "mdi-archive", "Saved copies of web pages", href: $"https://history.{settings.Shared.DuckDnsDomain}");

        // DO AFTER START: chown -R 1654:1654 ../services-data/system/nostos
        // (image runs as the dotnet 'app' user and needs /data writable by it - do not override the container user)
        _ = builder.AddContainer("nostos", "ghcr.io/christian-gennari/nostos", "latest")
            .WithContainerName("reading")
            .WithBindMount(settings.Knowledge.DataPath("NOSTOS_DATA", "../services-data/system/nostos"), "/data")
            .WithLabPort(8080, "nostos")
            .WithHomepage("Knowledge", "Nostos", "mdi-book-open-variant", "Reading tracker", $"https://reading.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("siyuan", "b3log/siyuan", "latest")
            .WithContainerName("notes")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("SIYUAN_ACCESS_AUTH_CODE_BYPASS", settings.Knowledge.Value("SIYUAN_ACCESS_AUTH_CODE_BYPASS", "true"))
            .WithArgs("serve", "--workspace=/siyuan/workspace/")
            .WithBindMount(settings.Knowledge.DataPath("SIYUAN_DATA", "../services-data/system/siyuan"), "/siyuan/workspace")
            .WithLabPort(6806, "siyuan")
            .WithHomepage("Knowledge", "Notes", "mdi-notebook", "Personal notes", href: $"https://notes.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("affine", "ghcr.io/toeverything/affine", "stable")
            .WithContainerName("affine")
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3010")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://affine:{settings.Secret("AFFINE_DB_PASSWORD")}@affine-db:5432/affine"))
            .WithBindMount(settings.Knowledge.DataPath("AFFINE_DATA", "../services-data/system/affine"), "/data")
            .WithLabPort(3010, "affine")
            .WaitFor(dependencies.AffineDb)
            .WithHomepage("Knowledge", "Affine", "mdi-notebook", "Team foundation server", $"https://affine.{settings.Shared.DuckDnsDomain}");

        var outlineSecrets = settings.Knowledge.DataPath("OUTLINE_SECRETS", "../services-data/system/outline/secrets");
        _ = builder.AddContainer("outline", "outlinewiki/outline", "latest")
            .WithContainerName("dev-notes")
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://outline:{settings.Secret("OUTLINE_DB_PASSWORD")}@outline-db:5432/outline?sslmode=disable"))
            .WithEnvironment("REDIS_URL", "redis://outline-redis:6379")
            .WithEnvironment("URL", $"https://dev-notes.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("FILE_STORAGE", "local")
            .WithEnvironment("FILE_STORAGE_LOCAL_ROOT_DIR", "/var/lib/outline/data")
            .WithBindMount(settings.Knowledge.DataPath("OUTLINE_DATA", "../services-data/system/outline"), "/var/lib/outline/data")
            .WithLabPort(3000, "outline")
            .WithEnvironment("SECRET_KEY", settings.Secret("OUTLINE_SECRET_KEY", SecretKind.Hex32, Path.Combine(outlineSecrets, "outline_secret_key")))
            .WithEnvironment("UTILS_SECRET", settings.Secret("OUTLINE_UTILS_SECRET_KEY", SecretKind.Hex32, Path.Combine(outlineSecrets, "outline_utils_secret")))
            .WaitFor(dependencies.OutlineDb)
            .WaitFor(dependencies.OutlineRedis)
            .WithHomepage("Knowledge", "Dev notes", "mdi-file-document-multiple", "Team wiki and notes", href: $"https://dev-notes.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("bookstack", "ghcr.io/linuxserver/bookstack", "latest")
            .WithContainerName("wiki")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("APP_URL", $"https://wiki.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_HOST", "bookstack-db")
            .WithEnvironment("DB_PORT", "3306")
            .WithEnvironment("DB_DATABASE", "bookstack")
            .WithEnvironment("DB_USER", "bookstack")
            .WithEnvironment("DB_PASS", settings.Secret("BOOKSTACK_DB_PASSWORD"))
            .WithBindMount(settings.Knowledge.DataPath("BOOKSTACK_DATA", "../services-data/system/bookstack"), "/config")
            .WithLabPort(80, "bookstack")
            .WaitFor(dependencies.BookstackDb)
            .WithHomepage("Knowledge", "Wiki", "mdi-file-document-multiple", "Structured wiki", href: $"https://wiki.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("zensical", "zensical/zensical", "latest")
            .WithContainerName("docs2")
            .WithArgs("serve", "--dev-addr=0.0.0.0:8000")
            .WithBindMount(settings.Knowledge.DataPath("ZENSICAL_DOCS", "../services-data/public/docs2"), "/docs")
            .WithConfigFile("configs/zensical/mkdocs.yml", "/docs/mkdocs.yml")
            .WithConfigFile("configs/zensical/index.md", "/docs/docs/index.md")
            .WithLabPort(8000, "zensical")
            .WithHomepage("Knowledge", "Zensical", "mdi-book-open-page-variant", "Docs server", $"https://docs2.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("karakeep", "ghcr.io/karakeep-app/karakeep", "release")
            .WithContainerName("cabinet")
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("KARAKEEP_SECRET"))
            .WithEnvironment("NEXTAUTH_URL", $"https://cabinet.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("MEILI_ADDR", "http://karakeep-meilisearch:7700")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret("KARAKEEP_MEILI_KEY"))
            .WithEnvironment("BROWSER_WEB_URL", "http://karakeep-chrome:9222")
            .WithBindMount(settings.Knowledge.DataPath("KARAKEEP_DATA", "../services-data/system/karakeep/data"), "/data")
            .WithLabPort(3000, "karakeep")
            .WaitFor(dependencies.KarakeepMeilisearch)
            .WaitFor(dependencies.KarakeepChrome)
            .WithHomepage("Knowledge", "Cabinet", "mdi-bookmark-multiple", "Bookmarks with full-text search", href: $"https://cabinet.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("karakeep", "http://cabinet:3000", key: HomepageVar("KARAKEEP_WIDGET_KEY"));

        _ = builder.AddContainer("linkwarden", "ghcr.io/linkwarden/linkwarden", "latest")
            .WithContainerName("pocket")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://linkwarden:{settings.Secret("LINKWARDEN_DB_PASSWORD")}@linkwarden-db:5432/linkwarden"))
            .WithEnvironment("MEILI_HOST", "http://linkwarden-meilisearch:7700")
            .WithEnvironment("MEILI_URL", "http://linkwarden-meilisearch:7700")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret("LINKWARDEN_MEILI_KEY"))
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("LINKWARDEN_SECRET"))
            .WithEnvironment("NEXTAUTH_URL", $"https://pocket.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("STORAGE_FOLDER", "/data/data")
            .WithBindMount(Path.Combine(settings.Knowledge.DataPath("LINKWARDEN_DATA", "../services-data/system/linkwarden/data"), "data"), "/data/data")
            .WithLabPort(3000, "linkwarden")
            .WaitFor(dependencies.LinkwardenDb)
            .WaitFor(dependencies.LinkwardenMeilisearch)
            .WithHomepage("Knowledge", "Pocket", "mdi-link-variant", "Saved links and snapshots", href: $"https://pocket.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("linkwarden", "http://pocket:3000", key: HomepageVar("LINKWARDEN_WIDGET_KEY"));

        _ = builder.AddContainer("joplin", "joplin/server", "latest")
            .WithContainerName("joplin-sync")
            .WithEnvironment("APP_BASE_URL", $"https://joplin-sync.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("APP_PORT", "22300")
            .WithEnvironment("DB_CLIENT", "pg")
            .WithEnvironment("POSTGRES_HOST", "joplin-db")
            .WithEnvironment("POSTGRES_PORT", "5432")
            .WithEnvironment("POSTGRES_DATABASE", "joplin-sync")
            .WithEnvironment("POSTGRES_USER", "joplin-sync")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("JOPLIN_DB_PASSWORD"))
            .WithLabPort(22300, "joplin")
            .WaitFor(dependencies.JoplinDb)
            .WithHomepage("Knowledge", "Joplin", "si-joplin", "Notes sync server", $"https://joplin-sync.{settings.Shared.DuckDnsDomain}");
    }
}
