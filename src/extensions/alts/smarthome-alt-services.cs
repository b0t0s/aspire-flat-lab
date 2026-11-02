using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeAltServices
{
    public static void AddSmartHomeAltServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("homebox", "sysadminsmedia/homebox", "latest")
            .WithDefaultMode(LabMode.Auto)
            .WithContainerName("inventory")
            .WithEnvironment("HBOX_LOG_LEVEL", "info")
            .WithEnvironment("HBOX_LOG_FORMAT", "text")
            .WithBindMount(settings.SmartHome.DataPath("HOMEBOX_DATA", "../services-data/system/homebox"), "/data")
            .WithLabPort(7745, "homebox")
            .WithEnvironment("HBOX_AUTH_API_KEY_PEPPER", settings.Secret("HBOX_AUTH_API_KEY_PEPPER", SecretKind.Base64, Path.Combine(settings.SmartHome.DataPath("HOMEBOX_SECRETS", "../services-data/system/homebox/secrets"), "homebox_api_key_pepper")))
            .WithHomepage("Household", "Inventory", "mdi-package-variant-closed", href: $"https://inventory.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("homebox", "http://inventory:7745",
                username: HomepageVar("HOMEBOX_WIDGET_USERNAME"),
                password: HomepageVar("HOMEBOX_WIDGET_PASSWORD"));

        _ = builder.AddContainer("nodered", "nodered/node-red", "latest")
            .WithContainerName("flows")
            .WithBindMount(settings.SmartHome.DataPath("NODERED_DATA", "../services-data/system/nodered"), "/data")
            .WithLabPort(1880, "nodered")
            .WithHomepage("Household", "Flows", "mdi-sitemap", "Automation flows", $"https://flows.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("openemr", "openemr/openemr", "8.2.0")
            .WithContainerName("medical")
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

        _ = builder.AddContainer("gramps-web", "ghcr.io/gramps-project/gramps-webapi", "latest")
            .WithContainerName("genealogy")
            .WithEnvironment("GRAMPS_DATABASE_URI", ReferenceExpression.Create($"postgresql://gramps:{settings.Secret("GRAMPS_DB_PASSWORD")}@gramps-db:5432/gramps"))
            .WithEnvironment("GRAMPS_SECRET_KEY", settings.Secret("GRAMPS_SECRET", SecretKind.LongPassword))
            .WithLabPort(5000, "gramps-web")
            .WaitFor(dependencies["gramps-db"])
            .WithHomepage("Household", "Genealogy", "mdi-family-tree", href: $"https://genealogy.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("actualbudget", "actualbudget/actual-server", "latest")
            .WithContainerName("budget")
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "5006")
            .WithEnvironment("ACTUAL_USER_FILE", "/data/user-files.json")
            .WithEnvironment("ACTUAL_PASSWORD", settings.Secret("ACTUALBUDGET_PASSWORD"))
            .WithBindMount(settings.SmartHome.DataPath("ACTUALBUDGET_DATA", "../services-data/system/actualbudget"), "/data")
            .WithLabPort(5006, "actualbudget")
            .WithHomepage("Household", "Budget", "mdi-wallet", href: $"https://budget.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("wallos", "bellamy/wallos", "latest")
            .WithContainerName("subscriptions")
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_DATA", "../services-data/system/wallos"), "/var/www/html/db")
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_IMAGES", "../services-data/system/wallos/images"), "/var/www/html/images/logos/uploads")
            .WithLabPort(80, "wallos")
            .WithHomepage("Household", "Subscriptions", "mdi-credit-card", href: $"https://subscriptions.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("wallos", "http://subscriptions:80", key: HomepageVar("WALLOS_WIDGET_KEY"));

        _ = builder.AddContainer("managemeals", "ghcr.io/nightwhistler/managemeals", "latest")
            .WithContainerName("meals")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8089")
            .WithBindMount(settings.SmartHome.DataPath("MANAGEMEALS_DATA", "../services-data/system/managemeals"), "/app/data")
            .WithLabPort(8089, "managemeals")
            .WithHomepage("Household", "Meals", "mdi-silverware-fork-knife", "Recipe manager", $"https://meals.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("grocy", "ghcr.io/linuxserver/grocy", "latest")
            .WithContainerName("household")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("DISABLE_BROWSER_OPEN", "true")
            .WithBindMount(settings.SmartHome.DataPath("GROCY_DATA", "../services-data/system/grocy"), "/config")
            .WithLabPort(80, "grocy")
            .WithHomepage("Household", "Household", "mdi-home", href: $"https://household.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("changedetection", "dgtlmoon/changedetection.io", "latest")
            .WithContainerName("changedetection")
            .WithBindMount(settings.SmartHome.DataPath("CHANGEDETECTION_DATA", "../services-data/system/changedetection"), "/datastore")
            .WithLabPort(5000, "changedetection")
            .WithHomepage("Infrastructure", "ChangeDetection", "mdi-monitor-dashboard", "Page change detector", $"https://changedetection.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("changedetectionio", "http://changedetection:5000", key: HomepageVar("CHANGEDETECTION_WIDGET_KEY"));
    }
}
