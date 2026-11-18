using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
internal static class IrlServices
{
    public static void AddIrlServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("tradetally", "potentialmidas/tradetally", "latest")
            .WithContainerName("trades")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("DB_HOST", "tradetally-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_USER", "trader")
            .WithEnvironment("DB_NAME", "trades")
            .WithEnvironment("DB_PASSWORD", settings.Secret("TRADETALLY_DB_PASSWORD"))
            .WithEnvironment("JWT_SECRET", settings.Secret("TRADETALLY_JWT_SECRET"))
            .WithEnvironment("VITE_API_URL", $"https://trades.{settings.Shared.DuckDnsDomain}/api")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Irl.DataPath("TRADETALLY_UPLOADS", "../services-data/system/tradetally/uploads"), "/app/backend/uploads")
            .WithLabPort(80, "tradetally")
            .WaitFor(dependencies.TradetallyDb)
            .WithHomepage("Irl", "TradeTally", "mdi-chart-line", "Trading journal", $"https://trades.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("adventurelog", "ghcr.io/seanmorley15/adventurelog", "latest")
            .WithContainerName("adventures")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("SITE_URL", $"https://adventures.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("PORT", "3000")
            .WithEnvironment("PGHOST", "adventurelog-db")
            .WithEnvironment("POSTGRES_DB", "adventurelog")
            .WithEnvironment("POSTGRES_USER", "adventure")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("ADVENTURELOG_DB_PASSWORD"))
            .WithEnvironment("DJANGO_ADMIN_USERNAME", "admin")
            .WithEnvironment("DJANGO_ADMIN_PASSWORD", settings.Secret("ADVENTURELOG_ADMIN_PASSWORD"))
            .WithEnvironment("DJANGO_ADMIN_EMAIL", $"admin@{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DISABLE_REGISTRATION", "False")
            .WithBindMount(settings.Irl.DataPath("ADVENTURELOG_DATA", "../services-data/system/adventurelog/media"), "/code/media")
            .WithLabPort(3000, "adventurelog")
            .WaitFor(dependencies.AdventurelogDb)
            .WithHomepage("Irl", "Adventures", "mdi-bag-suitcase", "Trips and places visited", href: $"https://adventures.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("trailkeepr", dependencies.TrailkeeprBuild, "Dockerfile.ui")
            .WithContainerName("hiking")
            .WithDefaultMode(LabMode.Off)
            .WithLabPort(80, "trailkeepr")
            .WaitFor(dependencies.TrailkeeprApi)
            .WithHomepage("Irl", "Hiking", "hiking", "Trailkeepr hike tracker", $"https://hiking.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("dawarich", "freikin/dawarich", "latest")
            .WithContainerName("locations")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("RAILS_ENV", "production")
            .WithEnvironment("APPLICATION_HOSTS", "*")
            .WithBindMount(settings.SmartHome.DataPath("DAWARICH_DATA", "../services-data/system/dawarich"), "/usr/src/app/storage")
            .WithLabPort(3000, "dawarich")
            .WithHomepage("Irl", "Locations", "mdi-map-marker-radius", "Location history", href: $"https://locations.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("actualbudget", "actualbudget/actual-server", "latest")
            .WithContainerName("budget")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "5006")
            .WithEnvironment("ACTUAL_USER_FILE", "/data/user-files.json")
            .WithEnvironment("ACTUAL_PASSWORD", settings.Secret("ACTUALBUDGET_PASSWORD"))
            .WithBindMount(settings.SmartHome.DataPath("ACTUALBUDGET_DATA", "../services-data/system/actualbudget"), "/data")
            .WithLabPort(5006, "actualbudget")
            .WithHomepage("Household", "Budget", "mdi-wallet", "Household budget", href: $"https://budget.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("wallos", "bellamy/wallos", "latest")
            .WithContainerName("subscriptions")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_DATA", "../services-data/system/wallos"), "/var/www/html/db")
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_IMAGES", "../services-data/system/wallos/images"), "/var/www/html/images/logos/uploads")
            .WithLabPort(80, "wallos")
            .WithHomepage("Household", "Subscriptions", "mdi-credit-card", "Subscriptions and renewals", href: $"https://subscriptions.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("wallos", "http://subscriptions:80", key: HomepageVar("WALLOS_WIDGET_KEY"));

        _ = builder.AddContainer("resonand", "ghcr.io/resonand-app/resonand", "0.2.0")
            .WithContainerName("voices")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("RESONAND_SECRET_KEY", settings.Secret("RESONAND_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Entertainment.DataPath("RESONAND_DATA", "../services-data/system/resonand"), "/data")
            .WithLabPort(8000, "resonand")
            .WithHomepage("Media", "Resonand", "mdi-voicemail", "Voice archive", $"https://voices.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("healthlog", "ghcr.io/mbombeck/healthlog", "latest")
            .WithContainerName("health")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgresql://health:{settings.Secret("HEALTHLOG_DB_PASSWORD")}@healthlog-db:5432/healthlog?schema=public"))
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("HEALTHLOG_ENCRYPTION_KEY", SecretKind.Hex32))
            .WithEnvironment("API_TOKEN_HMAC_KEY", settings.Secret("HEALTHLOG_API_TOKEN_HMAC_KEY", SecretKind.Hex32))
            .WithEnvironment("NEXT_PUBLIC_APP_URL", $"https://health.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("APP_URL", $"https://health.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HEALTHLOG_PROCESS_TYPE", "all")
            .WithLabPort(3000, "healthlog")
            .WaitFor(dependencies.HealthlogDb)
            .WithHomepage("Irl", "HealthLog", "mdi-heart-pulse", "Health metrics", $"https://health.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("medikeep", "ghcr.io/afairgiant/medikeep", "latest")
            .WithContainerName("health-records")
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
            .WaitFor(dependencies.MedikeepDb)
            .WithHomepage("Irl", "Medical records", "mdi-medical-bag", "Personal medical records", $"https://health-records.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("openemr", "openemr/openemr", "8.6.0")
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
            .WaitFor(dependencies.OpenemrDb)
            .WithHomepage("Irl", "Medical practice", "mdi-hospital", "Medical practice records", href: $"https://medical.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithDefaultMode(LabMode.Manual)
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Irl", "Phonebook", "mdi-calendar-clock", "Calendars and contacts", href: $"https://phonebook.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("parenthese-aio", "ghcr.io/iliesallali/parenthese-aio", "latest")
            .WithContainerName("familytree")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.Entertainment.DataPath("PARENTHESE_DATA", "../services-data/system/parenthese"), "/data")
            .WithLabPort(80, "parenthese-aio")
            .WithHomepage("Irl", "Family tree", "mdi-family-tree", "Family tree", $"https://familytree.{settings.Shared.DuckDnsDomain}");

        _ = dependencies.WithGrampsWeb(builder.AddContainer("gramps-web", "ghcr.io/gramps-project/grampsweb", "latest"))
            .WithContainerName("genealogy")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(5000, "gramps-web")
            .WaitFor(dependencies.GrampsRedis)
            .WaitFor(dependencies.GrampsCelery)
            .WithHomepage("Irl", "Genealogy", "mdi-family-tree", "Family tree research", href: $"https://genealogy.{settings.Shared.DuckDnsDomain}");
    }
}
