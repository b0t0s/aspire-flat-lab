using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
internal static class IrlServices
{
    public static void AddIrlServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("tradetally", "potentialmidas/tradetally", "latest")
            .WithContainerName("tradetally")
            .WithDefaultMode(LabMode.Off)
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

        _ = builder.AddContainer("adventurelog", "seanmorley15/adventurelog", "latest")
            .WithContainerName("adventures")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DJANGO_SETTINGS_MODULE", "adventurelog.settings")
            .WithEnvironment("SECRET_KEY", settings.Secret("ADVENTURELOG_SECRET", SecretKind.LongPassword))
            .WithBindMount(settings.SmartHome.DataPath("ADVENTURELOG_DATA", "../services-data/system/adventurelog"), "/app/backend/data")
            .WithLabPort(8000, "adventurelog")
            .WithHomepage("Irl", "Adventures", "mdi-bag-suitcase", href: $"https://adventures.{settings.Shared.DuckDnsDomain}");

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
            .WithDefaultMode(LabMode.Off)
            .WithLabPort(80, "trailkeepr")
            .WaitFor(hikingApi)
            .WithHomepage("Irl", "Hiking", "hiking", "Trailkeepr hike tracker", $"https://hiking.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("dawarich", "freikin/dawarich", "latest")
            .WithContainerName("dawarich")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("RAILS_ENV", "production")
            .WithEnvironment("APPLICATION_HOSTS", "*")
            .WithBindMount(settings.SmartHome.DataPath("DAWARICH_DATA", "../services-data/system/dawarich"), "/usr/src/app/storage")
            .WithLabPort(3000, "dawarich")
            .WithHomepage("Irl", "Location", "mdi-map-marker-radius", href: $"https://locations.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("actualbudget", "actualbudget/actual-server", "latest")
            .WithContainerName("budget")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "5006")
            .WithEnvironment("ACTUAL_USER_FILE", "/data/user-files.json")
            .WithEnvironment("ACTUAL_PASSWORD", settings.Secret("ACTUALBUDGET_PASSWORD"))
            .WithBindMount(settings.SmartHome.DataPath("ACTUALBUDGET_DATA", "../services-data/system/actualbudget"), "/data")
            .WithLabPort(5006, "actualbudget")
            .WithHomepage("Household", "Budget", "mdi-wallet", href: $"https://budget.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("wallos", "bellamy/wallos", "latest")
            .WithContainerName("subscriptions")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_DATA", "../services-data/system/wallos"), "/var/www/html/db")
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_IMAGES", "../services-data/system/wallos/images"), "/var/www/html/images/logos/uploads")
            .WithLabPort(80, "wallos")
            .WithHomepage("Household", "Subscriptions", "mdi-credit-card", href: $"https://subscriptions.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("wallos", "http://subscriptions:80", key: HomepageVar("WALLOS_WIDGET_KEY"));

        _ = builder.AddContainer("resonand", "ghcr.io/resonand-app/resonand", "0.1.0")
            .WithContainerName("resonand")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("RESONAND_SECRET_KEY", settings.Secret("RESONAND_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Entertainment.DataPath("RESONAND_DATA", "../services-data/system/resonand"), "/data")
            .WithLabPort(8000, "resonand")
            .WithHomepage("Media", "Resonand", "mdi-voicemail", "Voice archive", $"https://resonand.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("healthlog", "ghcr.io/mbombeck/healthlog", "latest")
            .WithContainerName("healthlog")
            .WithDefaultMode(LabMode.Manual)
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

        _ = builder.AddContainer("medikeep", "ghcr.io/afairgiant/medikeep", "latest")
            .WithContainerName("medikeep")
            .WithDefaultMode(LabMode.Off)
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
            .WithHomepage("Irl", "Medical records", "mdi-medical-bag", "", $"https://medikeep.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("openemr", "openemr/openemr", "8.2.0")
            .WithContainerName("medical")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("MYSQL_HOST", "openemr-db")
            .WithEnvironment("MYSQL_ROOT_PASS", settings.Secret("OPENEMR_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_USER", "openemr")
            .WithEnvironment("MYSQL_PASS", settings.Secret("OPENEMR_DB_PASS"))
            .WithEnvironment("OE_USER", settings.SmartHome.Value("OPENEMR_ADMIN_USER", "admin"))
            .WithEnvironment("OE_PASS", settings.Secret("OPENEMR_ADMIN_PASS"))
            .WithBindMount(settings.SmartHome.DataPath("OPENEMR_LOG", "../services-data/system/openemr/log"), "/var/log")
            .WithBindMount(settings.SmartHome.DataPath("OPENEMR_SITES", "../services-data/system/openemr/sites"), "/var/www/localhost/htdocs/openemr/sites")
            .WithLabPort(80, "openemr-http")
            .WithLabPort(443, "openemr-https", "https")
            .WaitFor(dependencies["openemr-db"])
            .WithHomepage("Household", "Medical", "mdi-hospital", href: $"https://medical.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithDefaultMode(LabMode.Auto)
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Irl", "Phonebook", "mdi-calendar-clock", href: $"https://phonebook.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("parenthese-aio", "ghcr.io/iliesallali/parenthese-aio", "latest")
            .WithContainerName("parenthese")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.Entertainment.DataPath("PARENTHESE_DATA", "../services-data/system/parenthese"), "/data")
            .WithLabPort(80, "parenthese-aio")
            .WithHomepage("Irl", "Family tree", "mdi-family-tree", "", $"https://familytree.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("gramps-web", "ghcr.io/gramps-project/gramps-webapi", "latest")
            .WithContainerName("genealogy")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("GRAMPS_DATABASE_URI", ReferenceExpression.Create($"postgresql://gramps:{settings.Secret("GRAMPS_DB_PASSWORD")}@gramps-db:5432/gramps"))
            .WithEnvironment("GRAMPS_SECRET_KEY", settings.Secret("GRAMPS_SECRET", SecretKind.LongPassword))
            .WithLabPort(5000, "gramps-web")
            .WaitFor(dependencies["gramps-db"])
            .WithHomepage("Household", "Genealogy", "mdi-family-tree", href: $"https://genealogy.{settings.Shared.DuckDnsDomain}");
    }
}
