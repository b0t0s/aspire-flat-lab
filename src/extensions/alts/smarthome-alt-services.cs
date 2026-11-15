using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeAltServices
{
    public static void AddSmartHomeAltServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("homebox", "sysadminsmedia/homebox", "latest")
            .WithDefaultMode(LabMode.Off)
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
            .WithDefaultMode(LabMode.Off)
            .WithBindMount(settings.SmartHome.DataPath("NODERED_DATA", "../services-data/system/nodered"), "/data")
            .WithLabPort(1880, "nodered")
            .WithHomepage("Household", "Flows", "mdi-sitemap", "Automation flows", $"https://flows.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("managemeals", "ghcr.io/nightwhistler/managemeals", "latest")
            .WithContainerName("meals")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8089")
            .WithBindMount(settings.SmartHome.DataPath("MANAGEMEALS_DATA", "../services-data/system/managemeals"), "/app/data")
            .WithLabPort(8089, "managemeals")
            .WithHomepage("Household", "Meals", "mdi-silverware-fork-knife", "Recipe manager", $"https://meals.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("grocy", "ghcr.io/linuxserver/grocy", "latest")
            .WithContainerName("household")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("DISABLE_BROWSER_OPEN", "true")
            .WithBindMount(settings.SmartHome.DataPath("GROCY_DATA", "../services-data/system/grocy"), "/config")
            .WithLabPort(80, "grocy")
            .WithHomepage("Household", "Household", "mdi-home", href: $"https://household.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("changedetection", "dgtlmoon/changedetection.io", "latest")
            .WithContainerName("changedetection")
            .WithDefaultMode(LabMode.Off)
            .WithBindMount(settings.SmartHome.DataPath("CHANGEDETECTION_DATA", "../services-data/system/changedetection"), "/datastore")
            .WithLabPort(5000, "changedetection")
            .WithHomepage("Infrastructure", "ChangeDetection", "mdi-monitor-dashboard", "Page change detector", $"https://changedetection.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("changedetectionio", "http://changedetection:5000", key: HomepageVar("CHANGEDETECTION_WIDGET_KEY"));

        _ = builder.AddContainer("opencloud", "opencloudeu/opencloud-rolling", "latest")
            .WithContainerName("opencloud")
            .WithDefaultMode(LabMode.Off)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", "opencloud init || true; opencloud server")
            .WithEnvironment("OC_URL", $"https://opencloud.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("OC_INSECURE", "false")
            .WithEnvironment("PROXY_TLS", "false")
            .WithEnvironment("OC_LOG_LEVEL", "info")
            .WithEnvironment("IDM_CREATE_DEMO_USERS", "false")
            .WithEnvironment("IDM_ADMIN_PASSWORD", settings.Secret("OPENCLOUD_ADMIN_PASSWORD"))
            .WithBindMount(settings.SmartHome.DataPath("OPENCLOUD_CONFIG", "../services-data/system/opencloud/config"), "/etc/opencloud")
            .WithBindMount(settings.SmartHome.DataPath("OPENCLOUD_DATA", "../services-data/system/opencloud/data"), "/var/lib/opencloud")
            .WithLabPort(9200, "opencloud")
            .WithHomepage("Household", "OpenCloud", "mdi-cloud-outline", "File cloud (ownCloud Infinite Scale fork)", $"https://opencloud.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("oxicloud", "diocrafts/oxicloud", "latest")
            .WithContainerName("oxicloud")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("OXICLOUD_SERVER_HOST", "0.0.0.0")
            .WithEnvironment("OXICLOUD_SERVER_PORT", "8086")
            .WithEnvironment("OXICLOUD_BASE_URL", $"https://oxicloud.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("OXICLOUD_STORAGE_PATH", "/app/storage")
            .WithEnvironment("OXICLOUD_DB_CONNECTION_STRING", ReferenceExpression.Create($"postgres://oxicloud:{settings.Secret("OXICLOUD_DB_PASSWORD")}@oxicloud-db:5432/oxicloud"))
            .WithEnvironment("MIMALLOC_PURGE_DELAY", "0")
            .WithBindMount(settings.SmartHome.DataPath("OXICLOUD_DATA", "../services-data/system/oxicloud/storage"), "/app/storage")
            .WithLabPort(8086, "oxicloud")
            .WaitFor(dependencies.OxicloudDb)
            .WithHomepage("Household", "OxiCloud", "mdi-cloud-sync", "Lightweight Rust file cloud", $"https://oxicloud.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("syncthing", "syncthing/syncthing", "latest")
            .WithContainerName("syncthing")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithBindMount(settings.SmartHome.DataPath("SYNCTHING_DATA", "../services-data/system/syncthing"), "/var/syncthing")
            .WithBindMount(settings.SmartHome.DataPath("SYNCTHING_FOLDERS", "../services-data/public/sync"), "/var/syncthing/folders")
            .WithLabPort(8384, "syncthing")
            .WithPublishedPort(22000, 22000, "tcp")
            .WithPublishedPort(22000, 22000, "udp")
            .WithPublishedPort(21027, 21027, "udp")
            .WithHomepage("Household", "Syncthing", "si-syncthing", "Peer-to-peer folder sync", $"https://sync.{settings.Shared.DuckDnsDomain}");

        var seafileDataPath = settings.SmartHome.DataPath("SEAFILE_DATA", "../services-data/system/seafile");

        var seafile = builder.AddContainer("seafile", "seafileltd/seafile-mc", "latest")
            .WithContainerName("cloud")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DB_HOST", "seafile-db")
            .WithEnvironment("TIME_ZONE", settings.Shared.TimeZone)
            .WithEnvironment("SEAFILE_SERVER_LETSENCRYPT", "false")
            .WithEnvironment("SEAFILE_SERVER_HOSTNAME", settings.Shared.DuckDnsDomain)
            .WithEnvironment("DB_ROOT_PASSWD", settings.Secret("SEAFILE_DB_ROOT_PASS"))
            .WithEnvironment("SEAFILE_ADMIN_EMAIL", settings.SmartHome.Value("SEAFILE_ADMIN_EMAIL"))
            .WithEnvironment("SEAFILE_ADMIN_PASSWORD", settings.Secret("SEAFILE_ADMIN_PASS"))
            .WithBindMount(seafileDataPath, "/shared")
            .WithLabPort(80, "seafile")
            .WaitFor(dependencies.SeafileDb)
            .WaitFor(dependencies.SeafileMemcached)
            .WithHomepage("Household", "Cloud", "mdi-cloud", href: $"https://cloud.{settings.Shared.DuckDnsDomain}");

        // Runs once against the live Seafile: seahub_settings.py only exists after Seafile's own first start.
        _ = builder.AddContainer("seafile-auto-config", "docker", "cli")
            .WithContainerName("seafile-auto-config")
            .WithDefaultMode(LabMode.Off)
            .WithRestart("no")
            .WithEnvironment("DUCKDNS_DOMAIN", settings.Shared.DuckDnsDomain)
            .WithBindMount(seafileDataPath, "/shared")
            .WithDockerSocket()
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", string.Join("\n",
                "CFG=/shared/seafile/conf/seahub_settings.py",
                "for i in $(seq 1 100); do [ -f \"$CFG\" ] && break; sleep 3; done",
                "[ -f \"$CFG\" ] || { echo \"seahub_settings.py not found, giving up\"; exit 1; }",
                "sleep 5",
                "sed -i \"s|'LOCATION': 'memcached:11211'|'LOCATION': 'seafile-memcached:11211'|\" \"$CFG\"",
                "if ! grep -q CSRF_TRUSTED_ORIGINS \"$CFG\"; then",
                "  printf \"\\nSERVICE_URL = 'https://cloud.%s'\\nFILE_SERVER_ROOT = 'https://cloud.%s/seafhttp'\\nCSRF_TRUSTED_ORIGINS = ['https://cloud.%s']\\nSECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')\\n\" \"$DUCKDNS_DOMAIN\" \"$DUCKDNS_DOMAIN\" \"$DUCKDNS_DOMAIN\" >> \"$CFG\"",
                "  docker restart cloud",
                "fi"))
            .WaitFor(seafile);
    }
}
