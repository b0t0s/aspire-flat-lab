using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
internal static class IrlServices
{
    public static void AddIrlServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("parenthese-aio", "ghcr.io/iliesallali/parenthese-aio", "latest")
            .WithContainerName("parenthese")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
            .WithBindMount(settings.Entertainment.DataPath("PARENTHESE_DATA", "../services-data/system/parenthese"), "/data")
            .WithLabPort(80, "parenthese-aio")
            .WithHomepage("Entertainment", "Family tree", "mdi-family-tree", "Parenthèse genealogy", $"https://familytree.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("medikeep", "ghcr.io/afairgiant/medikeep", "latest")
            .WithContainerName("medikeep")
            .WithStartPolicy(settings.Irl.StartByDefault)
            .WithEnvironment("DB_HOST", "medikeep-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "medical_records")
            .WithEnvironment("DB_USER", "medapp")
            .WithEnvironment("DB_PASSWORD", settings.Secret("MEDIKEEP_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("MEDIKEEP_SECRET_KEY"))
            .WithEnvironment("DEBUG", "false")
            .WithEnvironment("ENABLE_API_DOCS", "false")
            .WithEnvironment("LOG_LEVEL", "INFO")
            .WithEnvironment("ENABLE_SSL", "false")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_UPLOADS", "../services-data/system/medikeep/uploads"), "/app/uploads")
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_LOGS", "../services-data/system/medikeep/logs"), "/app/logs")
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_BACKUPS", "../services-data/system/medikeep/backups"), "/app/backups")
            .WithLabPort(8000, "medikeep")
            .WaitFor(dependencies["medikeep-db"])
            .WithHomepage("Irl", "MediKeep", "mdi-medical-bag", "Medical records", $"https://medikeep.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("healthlog", "ghcr.io/mbombeck/healthlog", "latest")
            .WithContainerName("healthlog")
            .WithStartPolicy(settings.Irl.StartByDefault)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://healthlog:{settings.Secret("HEALTHLOG_DB_PASSWORD")}@healthlog-db:5432/healthlog?schema=public"))
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("HEALTHLOG_ENCRYPTION_KEY", SecretKind.Hex32))
            .WithEnvironment("API_TOKEN_HMAC_KEY", settings.Secret("HEALTHLOG_API_TOKEN_HMAC_KEY", SecretKind.Hex32))
            .WithEnvironment("NEXT_PUBLIC_APP_URL", $"https://healthlog.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("APP_URL", $"https://healthlog.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HEALTHLOG_PROCESS_TYPE", "all")
            .WithLabPort(3000, "healthlog")
            .WaitFor(dependencies["healthlog-db"])
            .WithHomepage("Irl", "HealthLog", "mdi-heart-pulse", "Health metrics", $"https://healthlog.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("tradetally", "potentialmidas/tradetally", "latest")
            .WithContainerName("tradetally")
            .WithStartPolicy(settings.Irl.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("DB_HOST", "tradetally-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_USER", "trader")
            .WithEnvironment("DB_NAME", "tradetally")
            .WithEnvironment("DB_PASSWORD", settings.Secret("TRADETALLY_DB_PASSWORD"))
            .WithEnvironment("JWT_SECRET", settings.Secret("TRADETALLY_JWT_SECRET"))
            .WithEnvironment("VITE_API_URL", $"https://tradetally.{settings.Shared.DuckDnsDomain}/api")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Irl.DataPath("TRADETALLY_UPLOADS", "../services-data/system/tradetally/uploads"), "/app/backend/uploads")
            .WithLabPort(80, "tradetally")
            .WaitFor(dependencies["tradetally-db"])
            .WithHomepage("Irl", "TradeTally", "mdi-chart-line", "Trading journal", $"https://tradetally.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithStartPolicy(true)
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Household", "Phonebook", "mdi-calendar-clock", href: $"https://phonebook.{settings.Shared.DuckDnsDomain}");

        var trailkeeprSource = settings.Irl.Value("TRAILKEEPR_SOURCE", "./upstream/Trailkeepr");
        if (Directory.Exists(Path.Combine(builder.AppHostDirectory, trailkeeprSource)))
        {
            var hikingDb = builder.AddDockerfile("trailkeepr-db", trailkeeprSource, "init/Dockerfile")
                .WithContainerName("hiking-db")
                .WithStartPolicy(settings.Irl.StartByDefault)
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("TRAILKEEPR_DB_PASSWORD"))
                .WithEnvironment("POSTGRES_DB", "hiking")
                .WithBindMount(settings.Irl.DataPath("TRAILKEEPR_DB_DATA", "../services-data/system/trailkeepr/postgres"), "/var/lib/postgresql/data")
                .WithHealthcheck("pg_isready -U postgres");

            var hikingApi = builder.AddDockerfile("trailkeepr-api", $"{trailkeeprSource}/api")
                .WithContainerName("hiking-api")
                .WithStartPolicy(settings.Irl.StartByDefault)
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("TRAILKEEPR_DB_PASSWORD"))
                .WithEnvironment("POSTGRES_DB", "hiking")
                .WithBindMount(settings.Irl.DataPath("TRAILKEEPR_UPLOADS", "../services-data/system/trailkeepr/uploads"), "/app/uploads")
                .WaitFor(hikingDb);

            _ = builder.AddDockerfile("trailkeepr", $"{trailkeeprSource}/ui")
                .WithContainerName("hiking")
                .WithStartPolicy(settings.Irl.StartByDefault)
                .WithLabPort(80, "trailkeepr")
                .WaitFor(hikingApi)
                .WithHomepage("Irl", "Hiking", "hiking", "Trailkeepr hike tracker", $"https://hiking.{settings.Shared.DuckDnsDomain}");
        }
    }
}
