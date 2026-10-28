using apphost.cs.src;
using static EnvironmentSettings;

internal static class DevelopmentServices
{
    public static void AddDevelopmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("onedev", "1dev/server", "latest")
            .WithContainerName("projects")
            .WithStartPolicy(true)
            .WithBindMount(settings.Development.DataPath("ONEDEV_DATA", "../services-data/system/onedev"), "/opt/onedev")
            .WithDockerSocket(isReadOnly: false)
            .WithLabPort(6610, "onedev-http")
            .WithLabPort(6611, "onedev-agent")
            .WithHomepage("Development", "Projects", "mdi-git", href: $"https://projects.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("opengist", "ghcr.io/thomiceli/opengist", "latest")
            .WithContainerName("snippets")
            .WithStartPolicy(true)
            .WithEnvironment("OG_LISTEN_HOST", "0.0.0.0")
            .WithEnvironment("OG_LISTEN_PORT", "6157")
            .WithEnvironment("OG_REPOSITORY_PATH", "/app/repositories")
            .WithEnvironment("OG_SECRET_KEY", settings.Secret("OPENGIST_SECRET"))
            .WithBindMount(settings.Development.DataPath("OPENGIST_DATA", "../services-data/system/opengist"), "/opengist")
            .WithBindMount(settings.Development.DataPath("OPENGIST_REPOS", "../services-data/system/opengist-repos"), "/app/repositories")
            .WithLabPort(6157, "opengist")
            .WithHomepage("Development", "Snippets", "mdi-source-repository", href: $"https://snippets.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("it-tools", "corentinth/it-tools", "latest")
            .WithContainerName("toolbox")
            .WithStartPolicy(true)
            .WithLabPort(80, "it-tools")
            .WithHomepage("Development", "Toolbox", "mdi-tools", href: $"https://tools.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("convertx", "ghcr.io/c4illin/convertx", "latest")
            .WithContainerName("converter")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("JWT_SECRET", settings.Secret("CONVERTX_JWT_SECRET"))
            .WithBindMount(settings.Development.DataPath("CONVERTX_DATA", "../services-data/system/convertx"), "/app/data")
            .WithLabPort(3000, "convertx")
            .WithHomepage("Development", "Converter", "mdi-file-convert", href: $"https://convert.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("excalidraw", "excalidraw/excalidraw", "latest")
            .WithContainerName("whiteboard")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithLabPort(80, "excalidraw")
            .WithHomepage("Development", "Whiteboard", "mdi-draw", href: $"https://whiteboard.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("atuin", "ghcr.io/atuinsh/atuin", "latest")
            .WithContainerName("atuin")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithArgs("start")
            .WithEnvironment("ATUIN_HOST", "0.0.0.0")
            .WithEnvironment("ATUIN_OPEN_REGISTRATION", "true")
            .WithEnvironment("ATUIN_DB_URI", ReferenceExpression.Create($"postgres://atuin:{settings.Secret("ATUIN_DB_PASSWORD")}@atuin-db:5432/atuin"))
            .WithBindMount(settings.Development.DataPath("ATUIN_CONFIG", "../services-data/system/atuin/config"), "/config")
            .WithLabPort(8888, "atuin")
            .WaitFor(dependencies["atuin-db"])
            .WithHomepage("Development", "Atuin", "mdi-console-line", "Shell history sync", $"https://atuin.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("excalidash-backend", "zimengxiong/excalidash-backend", "latest")
            .WithContainerName("excalidash-backend")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("DATABASE_PROVIDER", "sqlite")
            .WithEnvironment("DATABASE_URL", "file:/app/prisma/dev.db")
            .WithEnvironment("PORT", "8000")
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("AUTH_MODE", "local")
            .WithEnvironment("TRUST_PROXY", "false")
            .WithEnvironment("JWT_SECRET", settings.Secret("EXCALIDASH_JWT_SECRET"))
            .WithEnvironment("CSRF_SECRET", settings.Secret("EXCALIDASH_CSRF_SECRET"))
            .WithBindMount(settings.Development.DataPath("EXCALIDASH_DATA", "../services-data/system/excalidash"), "/app/prisma")
            .WithLabPort(8000, "excalidash-backend")
            .WithHomepage("Development", "ExcaliDash", "mdi-view-dashboard-outline", "Whiteboard dashboard", $"https://dashboards.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("excalidash", "zimengxiong/excalidash-frontend", "latest")
            .WithContainerName("excalidash")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("BACKEND_URL", "excalidash-backend:8000")
            .WithLabPort(8080, "excalidash")
            .WithHomepage("Development", "ExcaliDash UI", "mdi-view-dashboard", "Whiteboard dashboard", $"https://dashboards.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ideon", "ghcr.io/3xpyth0n/ideon", "latest")
            .WithContainerName("ideon")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("APP_URL", $"https://ideon.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TIMEZONE", settings.Shared.TimeZone)
            .WithEnvironment("LOG_LEVEL", "info")
            .WithEnvironment("DB_HOST", "ideon-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "ideon")
            .WithEnvironment("DB_USER", "ideon")
            .WithEnvironment("DB_PASS", settings.Secret("IDEON_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("IDEON_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Development.DataPath("IDEON_DATA", "../services-data/system/ideon"), "/app/storage")
            .WithLabPort(3000, "ideon")
            .WaitFor(dependencies["ideon-db"])
            .WithHomepage("Development", "Ideon", "mdi-drawing-box", "Visual workspace", $"https://ideon.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("usertour", "usertour/usertour", "latest")
            .WithContainerName("usertour")
            .WithStartPolicy(settings.Development.StartByDefault)
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
            .WaitFor(dependencies["usertour-db"])
            .WaitFor(dependencies["usertour-redis"])
            .WithHomepage("Development", "Usertour", "mdi-cursor-default-click-outline", "Onboarding flows", $"https://usertour.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("lightpanda", "lightpanda/browser", "latest")
            .WithContainerName("lightpanda")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithLabPort(9222, "lightpanda")
            .WithHomepage("Development", "Lightpanda", "mdi-web", "Headless browser (CDP)", $"https://lightpanda.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("obscura", "h4ckf0r0day/obscura", "latest")
            .WithContainerName("obscura")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("OBSCURA_CDP_TOKEN", settings.Secret("OBSCURA_CDP_TOKEN", SecretKind.Hex32))
            .WithLabPort(9222, "obscura")
            .WithHomepage("Development", "Obscura", "mdi-incognito", "Stealth headless browser (CDP)", $"https://obscura.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("container-hub", "ghcr.io/eznix86/docker-registry-ui", "latest")
            .WithContainerName("registry")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("REGISTRY_URL", settings.Development.Value("REGISTRY_URL"))
            .WithEnvironment("REGISTRY_AUTH", settings.Development.Value("REGISTRY_AUTH"))
            .WithLabPort(3000, "container-hub")
            .WithHomepage("Infrastructure", "Registry UI", "mdi-docker", "Container Hub", $"https://registry.{settings.Shared.DuckDnsDomain}");

        var calagopusRedis = builder.AddContainer("calagopus-redis", "ghcr.io/calagopus/valkey", "latest")
            .WithContainerName("calagopus-redis")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithArgs("--protected-mode", "no", "--save", "60", "1")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_REDIS_DATA", "../services-data/system/calagopus/redis"), "/data");

        var calagopusDb = builder.AddContainer("calagopus-db", "ghcr.io/calagopus/pgautoupgrade", "18-alpine")
            .WithContainerName("calagopus-db")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("POSTGRES_USER", "panel")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("CALAGOPUS_DB_PASSWORD"))
            .WithEnvironment("POSTGRES_DB", "panel")
            .WithEnvironment("PGDATA", "/data")
            .WithBindMount(settings.Development.DataPath("CALAGOPUS_DB_DATA", "../services-data/system/calagopus/postgres"), "/data")
            .WithHealthcheck("pg_isready -U panel");

        _ = builder.AddContainer("calagopus", "ghcr.io/calagopus/panel", "aio")
            .WithContainerName("panel")
            .WithStartPolicy(settings.Development.StartByDefault)
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
            .WaitFor(calagopusDb)
            .WaitFor(calagopusRedis)
            .WithHomepage("Development", "Game Panel", "mdi-gamepad-variant", "Calagopus game server panel", $"https://panel.{settings.Shared.DuckDnsDomain}");
    }
}
