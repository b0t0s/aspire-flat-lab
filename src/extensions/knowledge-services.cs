using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Knowledge" - host ports 12xxx (docs/ports.md).</summary>
public static class KnowledgeServices
{
    public static void AddKnowledgeServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var knowledge = settings.Knowledge;
        var tools = settings.Tools;
        var app = settings.Productivity;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("affine", "ghcr.io/toeverything/affine", "stable")
            .WithContainerName("affine")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3010")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://affine:{settings.Secret("AFFINE_DB_PASSWORD")}@affine-db:5432/affine"))
            .WithBindMount(app.DataPath("AFFINE_DATA", "../services-data/system/affine"), "/data")
            .WithLabPort(3010, "affine")
            .WaitFor(dependencies["affine-db"])
            .WithHomepage("Knowledge", "Affine", "mdi-notebook", "Team foundation server", $"https://affine.{domain}");

        builder.AddContainer("memos", "neosmemo/memos", "stable")
            .WithContainerName("favourites")
            .WithBindMount(knowledge.DataPath("MEMOS_DATA", "../services-data/system/memos"), "/var/opt/memos")
            .WithLabPort(5230, "memos")
            .WithHomepage("Knowledge", "Favourites", "mdi-note-multiple", "Microblog", $"https://favourites.{domain}");

        builder.AddContainer("ghost", "ghost", "5")
            .WithContainerName("blog")
            .WithEnvironment("database__client", "mysql")
            .WithEnvironment("database__connection__host", "ghost-db")
            .WithEnvironment("database__connection__user", "ghost")
            .WithEnvironment("database__connection__password", settings.Secret("GHOST_DB_PASS"))
            .WithEnvironment("database__connection__database", "ghost")
            .WithEnvironment("url", $"https://blog.{domain}")
            .WithBindMount(knowledge.DataPath("GHOST_DATA", "../services-data/system/ghost"), "/var/lib/ghost/content")
            .WithLabPort(2368, "ghost")
            .WaitFor(dependencies["ghost-db"])
            .WithHomepage("Knowledge", "Blog", "si-ghost", "Blog and posts", $"https://blog.{domain}");

        builder.AddContainer("faved", "denho/faved", "latest")
            .WithContainerName("bookmarks")
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("SECRET_KEY", settings.Secret("FAVED_SECRET"))
            .WithBindMount(knowledge.DataPath("FAVED_DATA", "../services-data/system/faved"), "/var/www/html/storage/")
            .WithLabPort(80, "faved")
            .WithHomepage("Knowledge", "Bookmarks", "si-pocket", href: $"https://bookmarks.{domain}");

        builder.AddContainer("stirling-pdf", "stirlingtools/stirling-pdf", "latest")
            .WithContainerName("documents")
            .WithStartPolicy(tools.StartByDefault)
            .WithEnvironment("LANGS", "en_US")
            .WithEnvironment("SYSTEM_DEFAULTLOCALE", "en-US")
            .WithEnvironment("UI_APPNAME", "Home Lab Documents")
            .WithEnvironment("UI_HOMEDESCRIPTION", "")
            .WithEnvironment("SECURITY_ENABLELOGIN", tools.Value("STIRLING_SECURITY_ENABLELOGIN", "false"))
            .WithEnvironment("SECURITY_INITIALLOGIN_USERNAME", tools.Value("STIRLING_USER"))
            .WithEnvironment("SECURITY_INITIALLOGIN_PASSWORD", settings.Secret("STIRLING_PASSWORD"))
            .WithBindMount(tools.DataPath("STIRLING_TESSDATA", "../services-data/system/stirling-pdf/tessdata"), "/usr/share/tessdata")
            .WithBindMount(tools.DataPath("STIRLING_CONFIGS", "../services-data/system/stirling-pdf/configs"), "/configs")
            .WithBindMount(tools.DataPath("STIRLING_CUSTOM", "../services-data/system/stirling-pdf/customFiles"), "/customFiles")
            .WithBindMount(tools.DataPath("STIRLING_LOGS", "../services-data/system/stirling-pdf/logs"), "/logs")
            .WithLabPort(8080, "stirling-pdf")
            .WithHomepage("Knowledge", "Documents", "mdi-file-pdf-box", href: $"https://documents.{domain}");

        builder.AddContainer("karakeep", "ghcr.io/karakeep-dev/karakeep", "latest")
            .WithContainerName("cabinet")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://karakeep:{settings.Secret("KARAKEEP_DB_PASSWORD")}@karakeep-db:5432/karakeep"))
            .WithEnvironment("REDIS_URL", "redis://karakeep-redis:6379")
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("KARAKEEP_SECRET"))
            .WithEnvironment("NEXTAUTH_URL", $"https://karakeep.{domain}")
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("MEILI_HOST", "http://karakeep-meilisearch:7700")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret("KARAKEEP_MEILI_KEY"))
            .WithLabPort(3000, "karakeep")
            .WaitFor(dependencies["karakeep-db"])
            .WaitFor(dependencies["karakeep-redis"])
            .WaitFor(dependencies["karakeep-meilisearch"])
            .WithHomepage("Knowledge", "Cabinet", "mdi-bookmark-multiple", href: $"https://karakeep.{domain}")
            .WithHomepageWidget("karakeep", "http://cabinet:3000", key: HomepageVar("KARAKEEP_WIDGET_KEY"));

        builder.AddContainer("linkwarden", "ghcr.io/linkwarden/linkwarden", "latest")
            .WithContainerName("pocket")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://linkwarden:{settings.Secret("LINKWARDEN_DB_PASSWORD")}@linkwarden-db:5432/linkwarden"))
            .WithEnvironment("MEILI_HOST", "http://linkwarden-meilisearch:7700")
            .WithEnvironment("MEILI_URL", "http://linkwarden-meilisearch:7700")
            .WithEnvironment("MEILI_MASTER_KEY", settings.Secret("LINKWARDEN_MEILI_KEY"))
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("LINKWARDEN_SECRET"))
            .WithEnvironment("NEXTAUTH_URL", $"https://pocket.{domain}")
            .WithEnvironment("STORAGE_FOLDER", "/data/data")
            .WithBindMount(Path.Combine(knowledge.DataPath("LINKWARDEN_DATA", "../services-data/system/linkwarden/data"), "data"), "/data/data")
            .WithLabPort(3000, "linkwarden")
            .WaitFor(dependencies["linkwarden-db"])
            .WaitFor(dependencies["linkwarden-meilisearch"])
            .WithHomepage("Knowledge", "Pocket", "mdi-link-variant", href: $"https://pocket.{domain}")
            .WithHomepageWidget("linkwarden", "http://pocket:3000", key: HomepageVar("LINKWARDEN_WIDGET_KEY"));

        var outlineSecrets = knowledge.DataPath("OUTLINE_SECRETS", "../services-data/system/outline/secrets");
        builder.AddContainer("outline", "outlinewiki/outline", "latest")
            .WithContainerName("dev-notes")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://outline:{settings.Secret("OUTLINE_DB_PASSWORD")}@outline-db:5432/outline?sslmode=disable"))
            .WithEnvironment("REDIS_URL", "redis://outline-redis:6379")
            .WithEnvironment("URL", $"https://docs.{domain}")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("FILE_STORAGE", "local")
            .WithEnvironment("FILE_STORAGE_LOCAL_ROOT_DIR", "/var/lib/outline/data")
            .WithBindMount(knowledge.DataPath("OUTLINE_DATA", "../services-data/system/outline"), "/var/lib/outline/data")
            .WithLabPort(3000, "outline")
            // Were generated by the old outline-init container; imported from its files on first run.
            .WithEnvironment("SECRET_KEY", settings.Secret("OUTLINE_SECRET_KEY", SecretKind.Hex32, Path.Combine(outlineSecrets, "outline_secret_key")))
            .WithEnvironment("UTILS_SECRET", settings.Secret("OUTLINE_UTILS_SECRET_KEY", SecretKind.Hex32, Path.Combine(outlineSecrets, "outline_utils_secret")))
            .WaitFor(dependencies["outline-db"])
            .WaitFor(dependencies["outline-redis"])
            .WithHomepage("Knowledge", "Dev notes", "mdi-file-document-multiple", href: $"https://docs.{domain}");

        builder.AddContainer("bookstack", "ghcr.io/linuxserver/bookstack", "latest")
            .WithContainerName("wiki")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("PUID", shared.Uid)
            .WithEnvironment("PGID", shared.Gid)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("APP_URL", $"https://wiki.{domain}")
            .WithEnvironment("DB_HOST", "bookstack-db")
            .WithEnvironment("DB_PORT", "3306")
            .WithEnvironment("DB_DATABASE", "bookstack")
            .WithEnvironment("DB_USER", "bookstack")
            .WithEnvironment("DB_PASS", settings.Secret("BOOKSTACK_DB_PASSWORD"))
            .WithBindMount(knowledge.DataPath("BOOKSTACK_DATA", "../services-data/system/bookstack"), "/config")
            .WithLabPort(80, "bookstack")
            .WaitFor(dependencies["bookstack-db"])
            .WithHomepage("Knowledge", "Wiki", "mdi-file-document-multiple", href: $"https://wiki.{domain}");

        builder.AddContainer("siyuan", "b3log/siyuan", "latest")
            .WithContainerName("notes")
            .WithEnvironment("PUID", shared.Uid)
            .WithEnvironment("PGID", shared.Gid)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("SIYUAN_ACCESS_AUTH_CODE_BYPASS", knowledge.Value("SIYUAN_ACCESS_AUTH_CODE_BYPASS", "true"))
            .WithArgs("serve", "--workspace=/siyuan/workspace/")
            .WithBindMount(knowledge.DataPath("SIYUAN_DATA", "../services-data/system/siyuan"), "/siyuan/workspace")
            .WithLabPort(6806, "siyuan")
            .WithHomepage("Knowledge", "Notes", "mdi-notebook", href: $"https://notes.{domain}");

        builder.AddContainer("keila", "pentacent/keila", "latest")
            .WithContainerName("news")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("PHX_HOST", $"news.{domain}")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"ecto://keila:{settings.Secret("KEILA_DB_PASSWORD")}@keila-db:5432/keila"))
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("KEILA_SECRET", SecretKind.LongPassword))
            .WithEnvironment("SMTP_HOST", "")
            .WithEnvironment("SMTP_USERNAME", "")
            .WithEnvironment("SMTP_PASSWORD", "")
            .WithBindMount(knowledge.DataPath("KEILA_DATA", "../services-data/system/keila"), "/app/data")
            .WithLabPort(4000, "keila")
            .WaitFor(dependencies["keila-db"])
            .WithHomepage("Knowledge", "News", "si-mailgun", href: $"https://news.{domain}");

        builder.AddContainer("calibre-web", "lscr.io/linuxserver/calibre-web", "latest")
            .WithContainerName("archive")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("PUID", shared.Uid)
            .WithEnvironment("PGID", shared.Gid)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithBindMount(knowledge.DataPath("CALIBRE_DATA", "../services-data/system/calibre-web"), "/config")
            .WithBindMount(knowledge.DataPath("CALIBRE_LIBRARY", "../services-data/public/books"), "/books")
            .WithLabPort(8083, "calibre-web")
            .WithHomepage("Knowledge", "Archive", "mdi-book-open-page-variant", href: $"https://archive.{domain}")
            .WithHomepageWidget("calibreweb", "http://archive:8083",
                username: HomepageVar("CALIBRE_WIDGET_USER"),
                password: HomepageVar("CALIBRE_WIDGET_PASSWORD"));

        builder.AddContainer("archivebox", "archivebox/archivebox", "latest")
            .WithContainerName("history")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("ALLOWED_HOSTS", "*")
            .WithEnvironment("ADMIN_USERNAME", knowledge.Value("ARCHIVEBOX_ADMIN_USER"))
            .WithEnvironment("ADMIN_PASSWORD", settings.Secret("ARCHIVEBOX_ADMIN_PASS"))
            .WithBindMount(knowledge.DataPath("ARCHIVEBOX_DATA", "../services-data/system/archivebox"), "/data")
            .WithLabPort(8000, "archivebox")
            .WithHomepage("Knowledge", "History", "mdi-archive", href: $"https://history.{domain}");

        builder.AddContainer("perplexica", "itzcrazykns/perplexica", "main")
            .WithContainerName("perplexica")
            .WithStartPolicy(app.StartByDefault)
            .WithBindMount(app.DataPath("PERPLEXICA_DATA", "../services-data/system/perplexica"), "/home/perplexica/data")
            .WithLabPort(3000, "perplexica")
            .WithHomepage("Knowledge", "Perplexica", "mdi-magnify", "AI search", $"https://perplexica.{domain}");

        builder.AddContainer("dawarich", "freikin/dawarich", "latest")
            .WithContainerName("dawarich")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("RAILS_ENV", "production")
            .WithEnvironment("APPLICATION_HOSTS", "*")
            .WithBindMount(knowledge.DataPath("DAWARICH_DATA", "../services-data/system/dawarich"), "/usr/src/app/storage")
            .WithLabPort(3000, "dawarich")
            .WithHomepage("Knowledge", "Location", "mdi-map-marker-radius", href: $"https://locations.{domain}");

        builder.AddContainer("adventurelog", "seanmorley15/adventurelog", "latest")
            .WithContainerName("adventures")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("DJANGO_SETTINGS_MODULE", "adventurelog.settings")
            .WithEnvironment("SECRET_KEY", settings.Secret("ADVENTURELOG_SECRET", SecretKind.LongPassword))
            .WithBindMount(knowledge.DataPath("ADVENTURELOG_DATA", "../services-data/system/adventurelog"), "/app/backend/data")
            .WithLabPort(8000, "adventurelog")
            .WithHomepage("Knowledge", "Adventures", "mdi-bag-suitcase", href: $"https://adventures.{domain}");
    }
}
