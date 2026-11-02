using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
internal static class IrlServices
{
    public static void AddIrlAltServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("tradetally", "potentialmidas/tradetally", "latest")
            .WithContainerName("tradetally")
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

        _ = builder.AddContainer("healthlog", "ghcr.io/mbombeck/healthlog", "latest")
            .WithContainerName("healthlog")
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

        _ = builder.AddContainer("adventurelog", "seanmorley15/adventurelog", "latest")
            .WithContainerName("adventures")
            .WithEnvironment("DJANGO_SETTINGS_MODULE", "adventurelog.settings")
            .WithEnvironment("SECRET_KEY", settings.Secret("ADVENTURELOG_SECRET", SecretKind.LongPassword))
            .WithBindMount(settings.SmartHome.DataPath("ADVENTURELOG_DATA", "../services-data/system/adventurelog"), "/app/backend/data")
            .WithLabPort(8000, "adventurelog")
            .WithHomepage("Knowledge", "Adventures", "mdi-bag-suitcase", href: $"https://adventures.{settings.Shared.DuckDnsDomain}");

        var trailkeeprBuild = builder.RepoPath("src/containers/trailkeepr");
        var hikingDb = builder.AddDockerfile("trailkeepr-db", trailkeeprBuild, "Dockerfile.db")
            .WithContainerName("postgis")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("TRAILKEEPR_DB_PASSWORD"))
            .WithEnvironment("POSTGRES_DB", "hiking")
            .WithBindMount(settings.Irl.DataPath("TRAILKEEPR_DB_DATA", "../services-data/system/trailkeepr/postgres"), "/var/lib/postgresql/data")
            .WithHealthcheck("pg_isready -U postgres");

        var hikingApi = builder.AddDockerfile("trailkeepr-api", trailkeeprBuild, "Dockerfile.api")
            .WithContainerName("hiking-api")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("TRAILKEEPR_DB_PASSWORD"))
            .WithEnvironment("POSTGRES_DB", "hiking")
            .WithBindMount(settings.Irl.DataPath("TRAILKEEPR_UPLOADS", "../services-data/system/trailkeepr/uploads"), "/app/uploads")
            .WaitFor(hikingDb);

        _ = builder.AddDockerfile("trailkeepr", trailkeeprBuild, "Dockerfile.ui")
            .WithContainerName("hiking")
            .WithLabPort(80, "trailkeepr")
            .WaitFor(hikingApi)
            .WithHomepage("Irl", "Hiking", "hiking", "Trailkeepr hike tracker", $"https://hiking.{settings.Shared.DuckDnsDomain}");
    }
}
