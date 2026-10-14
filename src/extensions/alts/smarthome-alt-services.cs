using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeAltServices
{
    public static void AddAltSmartHomeServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("olivetin", "jamesread/olivetin", "latest")
            .WithContainerName("olivetin")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithLab(lab => lab.User = $"{settings.Shared.Uid}:124")
            .WithGroupAdd("124")
            .WithEndpoint(int.Parse(settings.SmartHome.Value("OLIVETIN_PORT", "1337"), System.Globalization.CultureInfo.InvariantCulture), 1337, name: "olivetin")
            .WithBindMount(settings.SmartHome.DataPath("OLIVETIN_DATA", "../services-data/system/olivetin"), "/config")
            .WithConfigFile("configs/olivetin/config.yaml", "/config/config.yaml")
            .WithBindMount(settings.SmartHome.DataPath("OLIVETIN_ENTITIES", "configs/olivetin/entities"), "/config/entities")
            .WithDockerSocket(isReadOnly: false)
            .WithHomepage("Infrastructure", "OliveTin", "mdi-gamepad", "Buttons panel", $"https://control.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("gramps-web", "ghcr.io/gramps-project/gramps-webapi", "latest")
            .WithContainerName("genealogy")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("GRAMPS_DATABASE_URI", ReferenceExpression.Create($"postgresql://gramps:{settings.Secret("GRAMPS_DB_PASSWORD")}@gramps-db:5432/gramps"))
            .WithEnvironment("GRAMPS_SECRET_KEY", settings.Secret("GRAMPS_SECRET", SecretKind.LongPassword))
            .WithLabPort(5000, "gramps-web")
            .WaitFor(dependencies["gramps-db"])
            .WithHomepage("Household", "Genealogy", "mdi-family-tree", href: $"https://genealogy.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("openemr", "openemr/openemr", "8.2.0")
            .WithContainerName("medical")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
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

        _ = builder.AddContainer("actualbudget", "actualbudget/actual-server", "latest")
            .WithContainerName("budget")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "5006")
            .WithEnvironment("ACTUAL_USER_FILE", "/data/user-files.json")
            .WithEnvironment("ACTUAL_PASSWORD", settings.Secret("ACTUALBUDGET_PASSWORD"))
            .WithBindMount(settings.SmartHome.DataPath("ACTUALBUDGET_DATA", "../services-data/system/actualbudget"), "/data")
            .WithLabPort(5006, "actualbudget")
            .WithHomepage("Household", "Budget", "mdi-wallet", href: $"https://budget.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("wallos", "bellamy/wallos", "latest")
            .WithContainerName("subscriptions")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_DATA", "../services-data/system/wallos"), "/var/www/html/db")
            .WithBindMount(settings.SmartHome.DataPath("WALLOS_IMAGES", "../services-data/system/wallos/images"), "/var/www/html/images/logos/uploads")
            .WithLabPort(80, "wallos")
            .WithHomepage("Household", "Subscriptions", "mdi-credit-card", href: $"https://subscriptions.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("wallos", "http://subscriptions:80", key: HomepageVar("WALLOS_WIDGET_KEY"));

        _ = builder.AddContainer("nodered", "nodered/node-red", "latest")
            .WithContainerName("flows")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("NODERED_DATA", "../services-data/system/nodered"), "/data")
            .WithLabPort(1880, "nodered")
            .WithHomepage("Household", "Flows", "mdi-sitemap", "Automation flows", $"https://flows.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("managemeals", "ghcr.io/nightwhistler/managemeals", "latest")
            .WithContainerName("meals")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8089")
            .WithBindMount(settings.SmartHome.DataPath("MANAGEMEALS_DATA", "../services-data/system/managemeals"), "/app/data")
            .WithLabPort(8089, "managemeals")
            .WithHomepage("Household", "Meals", "mdi-silverware-fork-knife", "Recipe manager", $"https://meals.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("grocy", "ghcr.io/linuxserver/grocy", "latest")
            .WithContainerName("household")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("DISABLE_BROWSER_OPEN", "true")
            .WithBindMount(settings.SmartHome.DataPath("GROCY_DATA", "../services-data/system/grocy"), "/config")
            .WithLabPort(80, "grocy")
            .WithHomepage("Household", "Household", "mdi-home", href: $"https://household.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("dawarich", "freikin/dawarich", "latest")
            .WithContainerName("dawarich")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("RAILS_ENV", "production")
            .WithEnvironment("APPLICATION_HOSTS", "*")
            .WithBindMount(settings.SmartHome.DataPath("DAWARICH_DATA", "../services-data/system/dawarich"), "/usr/src/app/storage")
            .WithLabPort(3000, "dawarich")
            .WithHomepage("Knowledge", "Location", "mdi-map-marker-radius", href: $"https://locations.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("adventurelog", "seanmorley15/adventurelog", "latest")
            .WithContainerName("adventures")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("DJANGO_SETTINGS_MODULE", "adventurelog.settings")
            .WithEnvironment("SECRET_KEY", settings.Secret("ADVENTURELOG_SECRET", SecretKind.LongPassword))
            .WithBindMount(settings.SmartHome.DataPath("ADVENTURELOG_DATA", "../services-data/system/adventurelog"), "/app/backend/data")
            .WithLabPort(8000, "adventurelog")
            .WithHomepage("Knowledge", "Adventures", "mdi-bag-suitcase", href: $"https://adventures.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("changedetection", "dgtlmoon/changedetection.io", "latest")
            .WithContainerName("changedetection")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("CHANGEDETECTION_DATA", "../services-data/system/changedetection"), "/datastore")
            .WithLabPort(5000, "changedetection")
            .WithHomepage("Infrastructure", "ChangeDetection", "mdi-monitor-dashboard", "Page change detector", $"https://changedetection.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("changedetectionio", "http://changedetection:5000", key: HomepageVar("CHANGEDETECTION_WIDGET_KEY"));

        _ = builder.AddContainer("beszel-hub", "henrygd/beszel", "latest")
            .WithContainerName("metrics")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("BESZEL_DATA", "../services-data/system/beszel"), "/beszel_data")
            .WithLabPort(8090, "beszel-hub")
            .WithHomepage("Infrastructure", "Metrics", "mdi-server", href: $"https://metrics.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("beszel", "http://metrics:8090", username: HomepageVar("BESZEL_WIDGET_USERNAME"), password: HomepageVar("BESZEL_WIDGET_PASSWORD"), version: "2");

        _ = builder.AddContainer("plausible", "plausible/analytics", "latest")
            .WithContainerName("analytics")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{settings.Secret("PLAUSIBLE_DB_PASSWORD")}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD"))
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("BASE_URL", $"https://analytics.{settings.Shared.DuckDnsDomain}")
            .WithLabPort(8000, "plausible")
            .WaitFor(dependencies["plausible-db"])
            .WaitFor(dependencies["plausible-events"])
            .WaitFor(dependencies["plausible-clickhouse"])
            .WithHomepage("Infrastructure", "Analytics", "mdi-chart-line", href: $"https://analytics.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("stirling-pdf", "stirlingtools/stirling-pdf", "latest")
            .WithContainerName("pdf")
            .WithStartPolicy(settings.Knowledge.StartByDefault)
            .WithEnvironment("LANGS", "en_US")
            .WithEnvironment("SYSTEM_DEFAULTLOCALE", "en-US")
            .WithEnvironment("UI_APPNAME", "Lab PDF Editor")
            .WithEnvironment("UI_HOMEDESCRIPTION", "")
            .WithEnvironment("SECURITY_ENABLELOGIN", settings.Knowledge.Value("STIRLING_SECURITY_ENABLELOGIN", "false"))
            .WithEnvironment("SECURITY_INITIALLOGIN_USERNAME", settings.Knowledge.Value("STIRLING_USER"))
            .WithEnvironment("SECURITY_INITIALLOGIN_PASSWORD", settings.Secret("STIRLING_PASSWORD"))
            .WithBindMount(settings.Knowledge.DataPath("STIRLING_TESSDATA", "../services-data/system/stirling-pdf/tessdata"), "/usr/share/tessdata")
            .WithBindMount(settings.Knowledge.DataPath("STIRLING_CONFIGS", "../services-data/system/stirling-pdf/configs"), "/configs")
            .WithBindMount(settings.Knowledge.DataPath("STIRLING_CUSTOM", "../services-data/system/stirling-pdf/customFiles"), "/customFiles")
            .WithBindMount(settings.Knowledge.DataPath("STIRLING_LOGS", "../services-data/system/stirling-pdf/logs"), "/logs")
            .WithLabPort(8080, "stirling-pdf")
            .WithHomepage("Knowledge", "PDF", "mdi-file-pdf-box", href: $"https://pdf.{settings.Shared.DuckDnsDomain}");
    }
}
