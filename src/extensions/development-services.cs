using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Development" - host ports 11xxx (docs/ports.md).</summary>
public static class DevelopmentServices
{
    public static void AddDevelopmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var app = settings.Productivity;
        var dev = settings.Development;
        var tools = settings.Tools;
        var shared = settings.Shared;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("code-server", "linuxserver/code-server", "latest")
            .WithContainerName("ide")
            .WithStartPolicy(dev.StartByDefault)
            .WithEnvironment("PUID", shared.Uid)
            .WithEnvironment("PGID", shared.Gid)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithEnvironment("SUDO_PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithBindMount(dev.DataPath("CODESERVER_DATA", "../services-data/system/code-server/config"), "/config")
            .WithBindMount(dev.DataPath("CODESERVER_PROJECTS", "../services-data/public"), "/projects")
            .WithLabPort(8443, "code-server")
            .WithHomepage("Development", "IDE", "mdi-microsoft-visual-studio-code", href: $"https://code.{domain}");

        builder.AddContainer("onedev", "1dev/server", "latest")
            .WithContainerName("projects")
            .WithBindMount(dev.DataPath("ONEDEV_DATA", "../services-data/system/onedev"), "/opt/onedev")
            .WithDockerSocket(isReadOnly: false)
            .WithLabPort(6610, "onedev-http")
            .WithLabPort(6611, "onedev-agent")
            .WithHomepage("Development", "Projects", "mdi-git", href: $"https://projects.{domain}");

        builder.AddContainer("opengist", "ghcr.io/thomiceli/opengist", "latest")
            .WithContainerName("snippets")
            .WithEnvironment("OG_LISTEN_HOST", "0.0.0.0")
            .WithEnvironment("OG_LISTEN_PORT", "6157")
            .WithEnvironment("OG_REPOSITORY_PATH", "/app/repositories")
            .WithEnvironment("OG_SECRET_KEY", settings.Secret("OPENGIST_SECRET"))
            .WithBindMount(dev.DataPath("OPENGIST_DATA", "../services-data/system/opengist"), "/app/opengist-data")
            .WithBindMount(dev.DataPath("OPENGIST_REPOS", "../services-data/system/opengist-repos"), "/app/repositories")
            .WithLabPort(6157, "opengist")
            .WithHomepage("Development", "Snippets", "mdi-source-repository", href: $"https://snippets.{domain}");

        builder.AddContainer("web-check", "lissy93/web-check", "latest")
            .WithContainerName("web-check")
            .WithStartPolicy(dev.StartByDefault)
            .WithLabPort(3000, "web-check")
            .WithHomepage("Development", "Web Check", "mdi-shield-search", href: $"https://webcheck.{domain}");

        builder.AddContainer("it-tools", "corentinth/it-tools", "latest")
            .WithContainerName("toolbox")
            .WithLabPort(80, "it-tools")
            .WithHomepage("Development", "Toolbox", "mdi-tools", href: $"https://tools.{domain}");

        // Placeholder image: set CODING_TOOLBOX_IMAGE to your own build before starting it.
        builder.AddContainer("coding-toolbox", tools.Value("CODING_TOOLBOX_IMAGE", "YOUR_ORG/coding-toolbox"), "latest")
            .WithContainerName("dev-tools")
            .WithStartPolicy(false)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("HOST", "0.0.0.0")
            .WithLabPort(3000, "coding-toolbox")
            .WithHomepage("Development", "DevTools", "mdi-toolbox", href: $"https://devtools.{domain}");

        var kuttDbPassword = settings.Secret("KUTT_DB_PASSWORD");
        builder.AddContainer("kutt", "ghcr.io/thedevs-network/kutt", "latest")
            .WithContainerName("shortener")
            .WithStartPolicy(tools.StartByDefault)
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://kutt:{kuttDbPassword}@kutt-db:5432/kutt"))
            .WithEnvironment("REDIS_URL", "redis://kutt-redis:6379")
            .WithEnvironment("DEFAULT_DOMAIN", $"go.{domain}")
            .WithEnvironment("LINK_LENGTH", "6")
            .WithLabPort(3000, "kutt")
            .WaitFor(dependencies["kutt-db"])
            .WaitFor(dependencies["kutt-redis"])
            .WithHomepage("Development", "URL shortener", "mdi-link-variant", href: $"https://go.{domain}");

        builder.AddContainer("convertx", "ghcr.io/c4illin/convertx", "latest")
            .WithContainerName("converter")
            .WithStartPolicy(dev.StartByDefault)
            .WithEnvironment("JWT_SECRET", settings.Secret("CONVERTX_JWT_SECRET"))
            .WithBindMount(dev.DataPath("CONVERTX_DATA", "../services-data/system/convertx"), "/app/data")
            .WithLabPort(3000, "convertx")
            .WithHomepage("Development", "Converter", "mdi-file-convert", href: $"https://convert.{domain}");

        builder.AddContainer("whois", "jinzeyang/whois", "latest")
            .WithContainerName("whois")
            .WithStartPolicy(dev.StartByDefault)
            .WithEnvironment("WHOIS_PORT", "8043")
            .WithEnvironment("WHOIS_RATE_LIMIT", "100")
            .WithEnvironment("WHOIS_LOG_LEVEL", "info")
            .WithEnvironment("WHOIS_CACHE_EXPIRATION", "3600")
            .WithEnvironment("WHOIS_NEGATIVE_CACHE_EXPIRATION", "60")
            .WithEnvironment("WHOIS_REQUIRE_REDIS", "false")
            .WithEnvironment("WHOIS_MEMORY_MAX_SIZE", "10000")
            .WithEnvironment("WHOIS_MEMORY_CLEAN_INTERVAL", "300")
            .WithEnvironment("WHOIS_AUTH_KEYS", settings.Secret("WHOIS_API_KEY"))
            .WithLabPort(8043, "whois")
            .WithHomepage("Development", "Whois", "mdi-account-search", "WHOIS lookup", $"https://whois.{domain}");

        builder.AddContainer("excalidraw", "excalidraw/excalidraw", "latest")
            .WithContainerName("whiteboard")
            .WithStartPolicy(tools.StartByDefault)
            .WithLabPort(80, "excalidraw")
            .WithHomepage("Development", "Whiteboard", "mdi-draw", href: $"https://whiteboard.{domain}");

        builder.AddContainer("super-productivity", "ghcr.io/super-productivity/supersync", "latest")
            .WithContainerName("super-productivity")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "1900")
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("RUN_MIGRATIONS_ON_STARTUP", "true")
            .WithEnvironment("JWT_SECRET", settings.Secret("SUPERPRODUCTIVITY_SYNC_SECRET"))
            .WithEnvironment("LOG_LEVEL", "info")
            .WithEnvironment("CORS_ENABLED", "true")
            .WithEnvironment("CORS_ORIGINS", $"https://tasks.{domain}")
            .WithEnvironment("WEBAUTHN_RP_NAME", "Super Productivity")
            .WithBindMount(app.DataPath("SUPERPRODUCTIVITY_DATA", "../services-data/system/super-productivity"), "/data")
            .WithLabPort(1900, "super-productivity")
            .WithHomepage("Development", "Super Productivity", "mdi-checkbox-marked-circle-outline", "Task tracker", $"https://tasks.{domain}");

        builder.AddContainer("solidtime", "solidtime/solidtime", "latest")
            .WithContainerName("solidtime")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_DEBUG", "false")
            .WithEnvironment("APP_KEY", settings.Secret("SOLIDTIME_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithBindMount(app.DataPath("SOLIDTIME_DATA", "../services-data/system/solidtime"), "/var/www/html/database")
            .WithLabPort(80, "solidtime")
            .WithHomepage("Development", "Solidtime", "mdi-clock-outline", "Time tracking", $"https://time.{domain}");

        builder.AddContainer("kimai", "kimai/kimai2", "latest")
            .WithContainerName("kimai")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("APP_ENV", "prod")
            .WithEnvironment("TRUSTED_PROXIES", "127.0.0.1,REMOTE_ADDR")
            .WithEnvironment("DATABASE_URL", "sqlite:///%kernel.project_dir%/var/data/kimai.sqlite")
            .WithBindMount(app.DataPath("KIMAI_DATA", "../services-data/system/kimai"), "/opt/kimai/var")
            .WithLabPort(80, "kimai")
            .WithHomepage("Development", "Kimai", "mdi-clock-outline", "Invoicing", $"https://time-kimai.{domain}");
    }
}
