using apphost.cs.src;
using static EnvironmentSettings;

internal static class DevelopmentAltServices
{
    public static void AddAltDevelopmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("code-server", "linuxserver/code-server", "latest")
            .WithContainerName("ide")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithEnvironment("SUDO_PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithBindMount(settings.Development.DataPath("CODESERVER_DATA", "../services-data/system/code-server/config"), "/config")
            .WithBindMount(settings.Development.DataPath("CODESERVER_PROJECTS", "../services-data/public"), "/projects")
            .WithLabPort(8443, "code-server")
            .WithHomepage("Development", "IDE", "mdi-microsoft-visual-studio-code", href: $"https://code.{settings.Shared.DuckDnsDomain}");

        // Placeholder image: set CODING_TOOLBOX_IMAGE to your own build before starting it.
        _ = builder.AddContainer("coding-toolbox", settings.Development.Value("CODING_TOOLBOX_IMAGE", "YOUR_ORG/coding-toolbox"), "latest")
            .WithContainerName("dev-tools")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("HOST", "0.0.0.0")
            .WithLabPort(3000, "coding-toolbox")
            .WithHomepage("Development", "DevTools", "mdi-toolbox", href: $"https://devtools.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("web-check", "lissy93/web-check", "latest")
            .WithContainerName("web-check")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithLabPort(3000, "web-check")
            .WithHomepage("Development", "Web Check", "mdi-shield-search", href: $"https://webcheck.{settings.Shared.DuckDnsDomain}");

        var kuttDbPassword = settings.Secret("KUTT_DB_PASSWORD");
        _ = builder.AddContainer("kutt", "ghcr.io/thedevs-network/kutt", "latest")
            .WithContainerName("shortener")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://kutt:{kuttDbPassword}@kutt-db:5432/kutt"))
            .WithEnvironment("REDIS_URL", "redis://kutt-redis:6379")
            .WithEnvironment("DEFAULT_DOMAIN", $"go.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("LINK_LENGTH", "6")
            .WithLabPort(3000, "kutt")
            .WaitFor(dependencies["kutt-db"])
            .WaitFor(dependencies["kutt-redis"])
            .WithHomepage("Development", "URL shortener", "mdi-link-variant", href: $"https://go.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("whois", "jinzeyang/whois", "latest")
            .WithContainerName("whois")
            .WithStartPolicy(settings.Development.StartByDefault)
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
            .WithHomepage("Development", "Whois", "mdi-account-search", "WHOIS lookup", $"https://whois.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("super-productivity", "ghcr.io/super-productivity/supersync", "latest")
            .WithContainerName("super-productivity")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "1900")
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("RUN_MIGRATIONS_ON_STARTUP", "true")
            .WithEnvironment("JWT_SECRET", settings.Secret("SUPERPRODUCTIVITY_SYNC_SECRET"))
            .WithEnvironment("LOG_LEVEL", "info")
            .WithEnvironment("CORS_ENABLED", "true")
            .WithEnvironment("CORS_ORIGINS", $"https://tasks.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("WEBAUTHN_RP_NAME", "Super Productivity")
            .WithBindMount(settings.Development.DataPath("SUPERPRODUCTIVITY_DATA", "../services-data/system/super-productivity"), "/data")
            .WithLabPort(1900, "super-productivity")
            .WithHomepage("Development", "Super Productivity", "mdi-checkbox-marked-circle-outline", "Task tracker", $"https://tasks.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("solidtime", "solidtime/solidtime", "latest")
            .WithContainerName("solidtime")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_DEBUG", "false")
            .WithEnvironment("APP_KEY", settings.Secret("SOLIDTIME_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithBindMount(settings.Development.DataPath("SOLIDTIME_DATA", "../services-data/system/solidtime"), "/var/www/html/database")
            .WithLabPort(80, "solidtime")
            .WithHomepage("Development", "Solidtime", "mdi-clock-outline", "Time tracking", $"https://time.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("kimai", "kimai/kimai2", "latest")
            .WithContainerName("kimai")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("APP_ENV", "prod")
            .WithEnvironment("TRUSTED_PROXIES", "127.0.0.1,REMOTE_ADDR")
            .WithEnvironment("DATABASE_URL", "sqlite:///%kernel.project_dir%/var/data/kimai.sqlite")
            .WithBindMount(settings.Development.DataPath("KIMAI_DATA", "../services-data/system/kimai"), "/opt/kimai/var")
            .WithLabPort(80, "kimai")
            .WithHomepage("Development", "Kimai", "mdi-clock-outline", "Invoicing", $"https://time-kimai.{settings.Shared.DuckDnsDomain}");
    }
}
