using apphost.cs.src;
using static EnvironmentSettings;

internal static class DevelopmentAltServices
{
    public static void AddDevelopmentAltServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("lightpanda", "lightpanda/browser", "latest")
            .WithContainerName("browser")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(9222, "lightpanda")
            .WithHomepage("Development", "Lightpanda", "mdi-web", "Headless browser (CDP)", $"https://browser.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("code-server", "linuxserver/code-server", "latest")
            .WithContainerName("ide")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithEnvironment("SUDO_PASSWORD", settings.Secret("CODESERVER_PASSWORD"))
            .WithBindMount(settings.Development.DataPath("CODESERVER_DATA", "../services-data/system/code-server/config"), "/config")
            .WithBindMount(settings.Development.DataPath("CODESERVER_PROJECTS", "../services-data/public"), "/projects")
            .WithLabPort(8443, "code-server")
            .WithHomepage("Development", "IDE", "mdi-microsoft-visual-studio-code", "VS Code in the browser", href: $"https://code.{settings.Shared.DuckDnsDomain}");

        // Placeholder image: set CODING_TOOLBOX_IMAGE to your own build before starting it.
        _ = builder.AddContainer("coding-toolbox", settings.Development.Value("CODING_TOOLBOX_IMAGE", "YOUR_ORG/coding-toolbox"), "latest")
            .WithContainerName("dev-tools")
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("HOST", "0.0.0.0")
            .WithLabPort(3000, "coding-toolbox")
            .WithHomepage("Development", "DevTools", "mdi-toolbox", "Own developer tools", href: $"https://dev-tools.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("usertour", "usertour/usertour", "latest")
            .WithContainerName("onboarding")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://postgres:{settings.Secret("USERTOUR_DB_PASSWORD")}@usertour-db:5432/usertour?schema=usertour"))
            .WithEnvironment("DATABASE_DIRECT_URL", ReferenceExpression.Create($"postgresql://postgres:{settings.Secret("USERTOUR_DB_PASSWORD")}@usertour-db:5432/usertour?schema=usertour"))
            .WithEnvironment("Redis_HOST", "usertour-redis")
            .WithEnvironment("Redis_PORT", "6379")
            .WithEnvironment("JWT_SECRET", settings.Secret("USERTOUR_JWT_SECRET"))
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("EMAIL_AUTH_ENABLED", "true")
            .WithEnvironment("LOGIN_REDIRECT_URL", "/env/1/flows")
            .WithEnvironment("USERTOUR_COOKIE_DOMAIN", settings.Shared.DuckDnsDomain)
            .WithLabPort(80, "usertour")
            .WaitFor(dependencies.UsertourDb)
            .WaitFor(dependencies.UsertourRedis)
            .WithHomepage("Development", "Usertour", "mdi-cursor-default-click-outline", "Onboarding flows", $"https://onboarding.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("web-check", "lissy93/web-check", "latest")
            .WithContainerName("webcheck")
            .WithLabPort(3000, "web-check")
            .WithHomepage("Development", "Web Check", "mdi-shield-search", "Inspect any website", href: $"https://webcheck.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("obscura", "h4ckf0r0day/obscura", "latest")
            .WithContainerName("stealth-browser")
            .WithEnvironment("OBSCURA_CDP_TOKEN", settings.Secret("OBSCURA_CDP_TOKEN", SecretKind.Hex32))
            .WithLabPort(9222, "obscura")
            .WithHomepage("Development", "Obscura", "mdi-incognito", "Stealth headless browser (CDP)", $"https://stealth-browser.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("whois", "jinzeyang/whois", "latest")
            .WithContainerName("whois")
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

        _ = builder.AddContainer("stirling-pdf", "stirlingtools/stirling-pdf", "latest")
            .WithContainerName("pdf")
            .WithEnvironment("LANGS", "en_US")
            .WithEnvironment("SYSTEM_DEFAULTLOCALE", "en-US")
            .WithEnvironment("UI_APPNAME", "Lab PDF Editor")
            .WithEnvironment("UI_HOMEDESCRIPTION", "")
            .WithEnvironment("SECURITY_ENABLELOGIN", settings.Development.Value("STIRLING_SECURITY_ENABLELOGIN", "false"))
            .WithEnvironment("SECURITY_INITIALLOGIN_USERNAME", settings.Development.Value("STIRLING_USER"))
            .WithEnvironment("SECURITY_INITIALLOGIN_PASSWORD", settings.Secret("STIRLING_PASSWORD"))
            .WithBindMount(settings.Development.DataPath("STIRLING_TESSDATA", "../services-data/system/stirling-pdf/tessdata"), "/usr/share/tessdata")
            .WithBindMount(settings.Development.DataPath("STIRLING_CONFIGS", "../services-data/system/stirling-pdf/configs"), "/configs")
            .WithBindMount(settings.Development.DataPath("STIRLING_CUSTOM", "../services-data/system/stirling-pdf/customFiles"), "/customFiles")
            .WithBindMount(settings.Development.DataPath("STIRLING_LOGS", "../services-data/system/stirling-pdf/logs"), "/logs")
            .WithLabPort(8080, "stirling-pdf")
            .WithHomepage("Development", "PDF", "mdi-file-pdf-box", "Edit, merge and convert PDFs", href: $"https://pdf.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("super-productivity", "ghcr.io/super-productivity/supersync", "latest")
            .WithContainerName("tasks")
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
            .WithContainerName("timesheet")
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_DEBUG", "false")
            .WithEnvironment("APP_KEY", settings.Secret("SOLIDTIME_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithBindMount(settings.Development.DataPath("SOLIDTIME_DATA", "../services-data/system/solidtime"), "/var/www/html/database")
            .WithLabPort(80, "solidtime")
            .WithHomepage("Development", "Solidtime", "mdi-clock-outline", "Time tracking", $"https://timesheet.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("kimai", "kimai/kimai2", "latest")
            .WithContainerName("invoicing")
            .WithEnvironment("APP_ENV", "prod")
            .WithEnvironment("TRUSTED_PROXIES", "127.0.0.1,REMOTE_ADDR")
            .WithEnvironment("DATABASE_URL", "sqlite:///%kernel.project_dir%/var/data/kimai.sqlite")
            .WithBindMount(settings.Development.DataPath("KIMAI_DATA", "../services-data/system/kimai"), "/opt/kimai/var")
            .WithLabPort(80, "kimai")
            .WithHomepage("Development", "Kimai", "mdi-clock-outline", "Invoicing", $"https://invoicing.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("plausible", "ghcr.io/plausible/community-edition", "v3.0.1")
            .WithContainerName("analytics")
            .WithArgs("sh", "-c", "/entrypoint.sh db createdb && /entrypoint.sh db migrate && /entrypoint.sh run")
            .WithEnvironment("BASE_URL", $"https://analytics.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("TMPDIR", "/var/lib/plausible/tmp")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{settings.Secret("PLAUSIBLE_DB_PASSWORD")}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", ReferenceExpression.Create($"http://plausible:{settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD")}@plausible-clickhouse:8123/plausible"))
            .WithBindMount(settings.Development.DataPath("PLAUSIBLE_DATA", "../services-data/system/plausible/data"), "/var/lib/plausible")
            .WithLabPort(8000, "plausible")
            .WaitFor(dependencies.PlausibleDb)
            .WaitFor(dependencies.PlausibleClickhouse)
            .WithHomepage("Infrastructure", "Analytics", "mdi-chart-line", "Website visitor statistics", href: $"https://analytics.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("beszel-hub", "henrygd/beszel", "latest")
            .WithContainerName("metrics")
            .WithBindMount(settings.SmartHome.DataPath("BESZEL_DATA", "../services-data/system/beszel"), "/beszel_data")
            .WithLabPort(8090, "beszel-hub")
            .WithHomepage("Infrastructure", "Metrics", "mdi-server", "Server resource history", href: $"https://metrics.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("beszel", "http://metrics:8090", username: HomepageVar("BESZEL_WIDGET_USERNAME"), password: HomepageVar("BESZEL_WIDGET_PASSWORD"), version: "2");

        _ = builder.AddContainer("beszel-agent", "henrygd/beszel-agent", "latest")
            .WithContainerName("beszel-agent")
            .WithEnvironment("PORT", "45876")
            .WithEnvironment("KEY", settings.Secret("BESZEL_AGENT_KEY", SecretKind.Manual))
            .WithDockerSocket()
            .WithHostNetwork();

        _ = builder.AddContainer("kutt", "kutt/kutt", "latest")
            .WithContainerName("shortener")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("DEFAULT_DOMAIN", $"go.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("JWT_SECRET", settings.Secret("KUTT_JWT_SECRET", SecretKind.LongPassword))
            .WithEnvironment("TRUST_PROXY", "true")
            .WithEnvironment("DB_CLIENT", "pg")
            .WithEnvironment("DB_HOST", "kutt-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "kutt")
            .WithEnvironment("DB_USER", "kutt")
            .WithEnvironment("DB_PASSWORD", settings.Secret("KUTT_DB_PASSWORD"))
            .WithEnvironment("REDIS_ENABLED", "true")
            .WithEnvironment("REDIS_HOST", "kutt-redis")
            .WithEnvironment("REDIS_PORT", "6379")
            .WithEnvironment("LINK_LENGTH", "6")
            .WithLabPort(3000, "kutt")
            .WaitFor(dependencies.KuttDb)
            .WaitFor(dependencies.KuttRedis)
            .WithHomepage("Development", "URL shortener", "mdi-link-variant", "Short links", href: $"https://go.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("calagopus", "ghcr.io/calagopus/panel", "aio")
            .WithContainerName("panel")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("REDIS_URL", "redis://calagopus-redis")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://panel:{settings.Secret("CALAGOPUS_DB_PASSWORD")}@calagopus-db/panel"))
            .WithEnvironment("DATABASE_MIGRATE", "true")
            .WithEnvironment("PORT", "8000")
            .WithEnvironment("AIO_BASE_WINGS_CONFIGURATION", "/wings-config.yml")
            .WithEnvironment("APP_DEBUG", "false")
            .WithEnvironment("APP_LOG_DIRECTORY", "/var/log/calagopus")
            .WithEnvironment("APP_PRIMARY", "true")
            .WithEnvironment("APP_ENABLE_WINGS_PROXY", "true")
            .WithEnvironment("APP_ENCRYPTION_KEY", settings.Secret("CALAGOPUS_APP_ENCRYPTION_KEY"))
            .WithEnvironment("APP_USE_DECRYPTION_CACHE", "true")
            .WithEnvironment("APP_USE_INTERNAL_CACHE", "true")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_DATA", "../services-data/system/calagopus"), "/var/lib/calagopus")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_LOGS", "../services-data/system/calagopus/logs"), "/var/log/calagopus")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_WINGS_CONFIG", "../services-data/system/calagopus/wings-config.yml"), "/wings-config.yml")
            .WithDockerSocket()
            .WithBindMount("/var/lib/docker/containers/", "/var/lib/docker/containers/")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_WINGS_ROOT", "../services-data/system/calagopus/wings"), "/var/lib/calagopus-wings/")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_WINGS_LOGS", "../services-data/system/calagopus/wings/logs"), "/var/log/calagopus-wings/")
            .WithBindMount("/etc/calagopus-wings/", "/etc/calagopus-wings/")
            .WithLab(lab => lab.ExtraPorts.Add((10031, 2022, "tcp")))
            .WithLabPort(8000, "calagopus")
            .WaitFor(dependencies.CalagopusDb)
            .WaitFor(dependencies.CalagopusRedis)
            .WithHomepage("Development", "Game Panel", "mdi-gamepad-variant", "Calagopus game server panel", $"https://panel.{settings.Shared.DuckDnsDomain}");

    }
}
