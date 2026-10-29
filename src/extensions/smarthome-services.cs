using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        var mySpeedHostPort = int.Parse(settings.SmartHome.Value("MYSPEED_PORT", "5216"), System.Globalization.CultureInfo.InvariantCulture);
        _ = builder.AddContainer("myspeed", "germannewsmaker/myspeed", "latest")
            .WithContainerName("speed")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("MYSPEED_DATA_PATH", "../services-data/system/myspeed"), "/config")
            .WithPublishedPort(mySpeedHostPort, 5216)
            .WithHomepage("Infrastructure", "Speed", "mdi-speedometer", href: $"https://speed.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("myspeed", "http://speed:5216");

        _ = builder.AddContainer("homebox", "sysadminsmedia/homebox", "latest")
            .WithStartPolicy(true)
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

        var termixDataPath = settings.SmartHome.DataPath("TERMIX_DATA", "../services-data/system/termix");

        var guacd = builder.AddContainer("guacd", "guacamole/guacd", "1.6.0")
            .WithContainerName("guacd")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(termixDataPath, "/termix-data");

        _ = builder.AddContainer("termix", "ghcr.io/termix-ssh/termix", "latest")
            .WithContainerName("termix")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("PORT", "8080")
            .WithEnvironment("GUACD_HOST", "guacd")
            .WithEnvironment("GUACD_TUNNEL_HOST", "termix")
            .WithEnvironment("GUACD_RECORDING_PATH", "/termix-data/session_recordings/guacamole")
            .WithEnvironment("GUACD_DRIVE_PATH", "/termix-data/rdp-drive")
            .WithBindMount(termixDataPath, "/app/data")
            .WithBindMount(termixDataPath, "/termix-data")
            .WithLabPort(8080, "termix")
            .WaitFor(guacd)
            .WithHomepage("Infrastructure", "SSH terminal", "mdi-console", "Termix web terminal", $"https://termix.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("degoog", "ghcr.io/degoog-org/degoog", "latest")
            .WithContainerName("degoog")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("DEGOOG_SETTINGS_PASSWORDS", settings.Secret("DEGOOG_SETTINGS_PASSWORDS", SecretKind.Manual))
            .WithBindMount(settings.SmartHome.DataPath("DEGOOG_DATA", "../services-data/system/degoog"), "/app/data")
            .WithLabPort(4444, "degoog")
            .WithHomepage("Infrastructure", "DeGoog", "mdi-magnify-expand", "Search aggregator", $"https://degoog.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("house-planner", builder.RepoPath("src/containers/house-planner"))
            .WithContainerName("floorplan")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("HOUSE_PASSWORD", settings.Secret("HOUSE_PLANNER_PASSWORD"))
            .WithEnvironment("HOUSE_SETUP_KEY", settings.SmartHome.Value("HOUSE_PLANNER_SETUP_KEY"))
            .WithEnvironment("HOUSE_DEMO", settings.SmartHome.Value("HOUSE_PLANNER_DEMO"))
            .WithBindMount(settings.SmartHome.DataPath("HOUSE_PLANNER_DATA", "../services-data/system/house-planner"), "/var/lib/house-planner")
            .WithLabPort(8080, "house-planner")
            .WithHomepage("Household", "Floor plan", "mdi-home-floor-a", "House planner", $"https://floorplan.{settings.Shared.DuckDnsDomain}");

        var worldmonitorRedis = builder.AddContainer("worldmonitor-redis", "redis", "7-alpine")
            .WithContainerName("worldmonitor-redis")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithArgs("redis-server", "--requirepass", settings.Secret("WORLDMONITOR_REDIS_PASSWORD"), "--maxmemory", "256mb", "--maxmemory-policy", "allkeys-lru")
            .WithBindMount(settings.SmartHome.DataPath("WORLDMONITOR_REDIS_DATA", "../services-data/system/worldmonitor/redis"), "/data");

        var worldmonitorRedisRest = builder.AddContainer("worldmonitor-redis-rest", "worldmonitor-redis-rest", "local")
            .WithContainerName("worldmonitor-redis-rest")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("SRH_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("SRH_CONNECTION_STRING", ReferenceExpression.Create($"redis://:{settings.Secret("WORLDMONITOR_REDIS_PASSWORD")}@worldmonitor-redis:6379"))
            .WaitFor(worldmonitorRedis);

        var worldmonitorRelay = builder.AddContainer("worldmonitor-ais-relay", "worldmonitor-ais-relay", "local")
            .WithContainerName("worldmonitor-ais-relay")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("RELAY_SHARED_SECRET", settings.Secret("WORLDMONITOR_RELAY_SHARED_SECRET"))
            .WithEnvironment("UPSTASH_REDIS_REST_URL", "http://worldmonitor-redis-rest:80")
            .WithEnvironment("UPSTASH_REDIS_REST_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("UPSTASH_ALLOW_INSECURE_HTTP", "true")
            .WithEnvironment("API_BASE_URL", "http://worldmonitor:8080")
            .WithEnvironment("PORT", "3004")
            .WaitFor(worldmonitorRedisRest);

        _ = builder.AddContainer("worldmonitor", "worldmonitor", "local")
            .WithContainerName("worldmonitor")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("UPSTASH_REDIS_REST_URL", "http://worldmonitor-redis-rest:80")
            .WithEnvironment("UPSTASH_REDIS_REST_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("WM_SESSION_SECRET", settings.Secret("WORLDMONITOR_SESSION_SECRET", SecretKind.Hex32))
            .WithEnvironment("LOCAL_API_PORT", "46123")
            .WithEnvironment("LOCAL_API_MODE", "docker")
            .WithEnvironment("LOCAL_API_CLOUD_FALLBACK", "false")
            .WithEnvironment("WS_RELAY_URL", "http://worldmonitor-ais-relay:3004")
            .WithEnvironment("RELAY_SHARED_SECRET", settings.Secret("WORLDMONITOR_RELAY_SHARED_SECRET"))
            .WithLabPort(8080, "worldmonitor")
            .WaitFor(worldmonitorRedisRest)
            .WaitFor(worldmonitorRelay)
            .WithHomepage("Entertainment", "WorldMonitor", "mdi-earth-box", "Global intelligence dashboard", $"https://worldmonitor.{settings.Shared.DuckDnsDomain}");
    }
}
