/// <summary>Homepage group "Household" - host ports 15xxx; its Infrastructure apps use 10xxx (docs/ports.md).</summary>
public static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var home = settings.SmartHome;
        var app = settings.Productivity;
        var knowledge = settings.Knowledge;
        var monitor = settings.Monitor;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithStartPolicy(app.StartByDefault)
            .WithBindMount(app.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(app.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(app.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Household", "Phonebook", "mdi-calendar-clock", href: $"https://phonebook.{domain}");

        builder.AddContainer("actualbudget", "actualbudget/actual-server", "latest")
            .WithContainerName("budget")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("PORT", "5006")
            .WithEnvironment("ACTUAL_USER_FILE", "/data/user-files.json")
            .WithEnvironment("ACTUAL_PASSWORD", settings.Secret("ACTUALBUDGET_PASSWORD"))
            .WithBindMount(app.DataPath("ACTUALBUDGET_DATA", "../services-data/system/actualbudget"), "/data")
            .WithLabPort(5006, "actualbudget")
            .WithHomepage("Household", "Budget", "mdi-wallet", href: $"https://budget.{domain}");

        builder.AddContainer("wallos", "bellamy/wallos", "latest")
            .WithContainerName("subscriptions")
            .WithStartPolicy(app.StartByDefault)
            .WithBindMount(app.DataPath("WALLOS_DATA", "../services-data/system/wallos"), "/var/www/html/db")
            .WithBindMount(app.DataPath("WALLOS_IMAGES", "../services-data/system/wallos/images"), "/var/www/html/images/logos/uploads")
            .WithLabPort(80, "wallos")
            .WithHomepage("Household", "Subscriptions", "mdi-credit-card", href: $"https://subscriptions.{domain}")
            .WithHomepageWidget("wallos", "http://subscriptions:80", key: HomepageVar("WALLOS_WIDGET_KEY"));

        builder.AddContainer("homebox", "sysadminsmedia/homebox", "latest")
            .WithContainerName("inventory")
            .WithEnvironment("HBOX_LOG_LEVEL", "info")
            .WithEnvironment("HBOX_LOG_FORMAT", "text")
            .WithBindMount(home.DataPath("HOMEBOX_DATA", "../services-data/system/homebox"), "/data")
            .WithLabPort(7745, "homebox")
            .WithEnvironment("HBOX_AUTH_API_KEY_PEPPER", settings.Secret("HBOX_AUTH_API_KEY_PEPPER", SecretKind.Base64,
                Path.Combine(home.DataPath("HOMEBOX_SECRETS", "../services-data/system/homebox/secrets"), "homebox_api_key_pepper")))
            .WithHomepage("Household", "Inventory", "mdi-package-variant-closed", href: $"https://inventory.{domain}")
            .WithHomepageWidget("homebox", "http://inventory:7745",
                username: HomepageVar("HOMEBOX_WIDGET_USERNAME"),
                password: HomepageVar("HOMEBOX_WIDGET_PASSWORD"));

        builder.AddContainer("nodered", "nodered/node-red", "latest")
            .WithContainerName("flows")
            .WithStartPolicy(home.StartByDefault)
            .WithBindMount(home.DataPath("NODERED_DATA", "../services-data/system/nodered"), "/data")
            .WithLabPort(1880, "nodered")
            .WithHomepage("Household", "Flows", "mdi-sitemap", "Automation flows", $"https://flows.{domain}");

        builder.AddContainer("paperless-ngx", "ghcr.io/paperless-ngx/paperless-ngx", "latest")
            .WithContainerName("checkouts")
            .WithStartPolicy(knowledge.StartByDefault)
            .WithEnvironment("PAPERLESS_REDIS", "redis://paperless-redis:6379")
            .WithEnvironment("PAPERLESS_DBENGINE", "postgresql")
            .WithEnvironment("PAPERLESS_DBHOST", "paperless-db")
            .WithEnvironment("PAPERLESS_DBNAME", "paperless")
            .WithEnvironment("PAPERLESS_DBUSER", "paperless")
            .WithEnvironment("PAPERLESS_DBPASS", settings.Secret("PAPERLESS_DB_PASSWORD"))
            .WithEnvironment("PAPERLESS_SECRET_KEY", settings.Secret("PAPERLESS_SECRET", SecretKind.LongPassword))
            .WithEnvironment("PAPERLESS_TIME_ZONE", shared.TimeZone)
            .WithEnvironment("PAPERLESS_URL", $"https://paperless.{domain}")
            .WithEnvironment("USERMAP_UID", shared.Uid)
            .WithEnvironment("USERMAP_GID", shared.Gid)
            .WithBindMount(knowledge.DataPath("PAPERLESS_DATA", "../services-data/system/paperless/data"), "/usr/src/paperless/data")
            .WithBindMount(knowledge.DataPath("PAPERLESS_MEDIA", "../services-data/system/paperless/media"), "/usr/src/paperless/media")
            .WithBindMount(knowledge.DataPath("PAPERLESS_EXPORT", "../services-data/system/paperless/export"), "/usr/src/paperless/export")
            .WithBindMount(knowledge.DataPath("PAPERLESS_CONSUME", "../services-data/system/paperless/consume"), "/usr/src/paperless/consume")
            .WithLabPort(8000, "paperless-ngx")
            .WaitFor(dependencies["paperless-redis"])
            .WaitFor(dependencies["paperless-db"])
            .WithHomepage("Knowledge", "Checkouts", "mdi-file-pdf-box", href: $"https://paperless.{domain}")
            .WithHomepageWidget("paperlessngx", "http://checkouts:8000", key: HomepageVar("PAPERLESS_WIDGET_KEY"));

        builder.AddContainer("managemeals", "ghcr.io/nightwhistler/managemeals", "latest")
            .WithContainerName("meals")
            .WithStartPolicy(home.StartByDefault)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8089")
            .WithBindMount(home.DataPath("MANAGEMEALS_DATA", "../services-data/system/managemeals"), "/app/data")
            .WithLabPort(8089, "managemeals")
            .WithHomepage("Household", "Meals", "mdi-silverware-fork-knife", "Recipe manager", $"https://meals.{domain}");

        builder.AddContainer("grocy", "ghcr.io/linuxserver/grocy", "latest")
            .WithContainerName("household")
            .WithStartPolicy(home.StartByDefault)
            .WithEnvironment("PUID", shared.Uid)
            .WithEnvironment("PGID", shared.Gid)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("DISABLE_BROWSER_OPEN", "true")
            .WithBindMount(home.DataPath("GROCY_DATA", "../services-data/system/grocy"), "/config")
            .WithLabPort(80, "grocy")
            .WithHomepage("Household", "Household", "mdi-home", href: $"https://household.{domain}");

        builder.AddContainer("changedetection", "dgtlmoon/changedetection.io", "latest")
            .WithContainerName("changedetection")
            .WithStartPolicy(monitor.StartByDefault)
            .WithBindMount(monitor.DataPath("CHANGEDETECTION_DATA", "../services-data/system/changedetection"), "/datastore")
            .WithLabPort(5000, "changedetection")
            .WithHomepage("Infrastructure", "ChangeDetection", "mdi-monitor-dashboard", "Page change detector", $"https://changedetection.{domain}")
            .WithHomepageWidget("changedetectionio", "http://changedetection:5000", key: HomepageVar("CHANGEDETECTION_WIDGET_KEY"));

        builder.AddContainer("beszel-hub", "henrygd/beszel", "latest")
            .WithContainerName("metrics")
            .WithStartPolicy(monitor.StartByDefault)
            .WithBindMount(monitor.DataPath("BESZEL_DATA", "../services-data/system/beszel"), "/beszel_data")
            .WithLabPort(8090, "beszel-hub")
            .WithHomepage("Infrastructure", "Metrics", "mdi-server", href: $"https://metrics.{domain}")
            .WithHomepageWidget("beszel", "http://metrics:8090", username: HomepageVar("BESZEL_WIDGET_USERNAME"), password: HomepageVar("BESZEL_WIDGET_PASSWORD"), version: "2");

        builder.AddContainer("plausible", "plausible/analytics", "latest")
            .WithContainerName("analytics")
            .WithStartPolicy(monitor.StartByDefault)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{settings.Secret("PLAUSIBLE_DB_PASSWORD")}@plausible-db:5432/plausible"))
            .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
            .WithEnvironment("CLICKHOUSE_USER", "plausible")
            .WithEnvironment("CLICKHOUSE_PASSWORD", settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD"))
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
            .WithEnvironment("BASE_URL", $"https://analytics.{domain}")
            .WithLabPort(8000, "plausible")
            .WaitFor(dependencies["plausible-db"])
            .WaitFor(dependencies["plausible-events"])
            .WaitFor(dependencies["plausible-clickhouse"])
            .WithHomepage("Infrastructure", "Analytics", "mdi-chart-line", href: $"https://analytics.{domain}");
    }
}
