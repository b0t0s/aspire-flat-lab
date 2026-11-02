using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder,  DependenciesService dependencies, EnvironmentSettings settings)
    {
        var mySpeedHostPort = int.Parse(settings.SmartHome.Value("MYSPEED_PORT", "5216"), System.Globalization.CultureInfo.InvariantCulture);
        _ = builder.AddContainer("myspeed", "germannewsmaker/myspeed", "latest")
            .WithContainerName("speed")
            .WithBindMount(settings.SmartHome.DataPath("MYSPEED_DATA_PATH", "../services-data/system/myspeed"), "/config")
            .WithPublishedPort(mySpeedHostPort, 5216)
            .WithHomepage("Infrastructure", "Speed", "mdi-speedometer", href: $"https://speed.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("myspeed", "http://speed:5216");

        _ = builder.AddDockerfile("house-planner", builder.RepoPath("src/containers/house-planner"))
            .WithContainerName("floorplan")
            .WithEnvironment("HOUSE_PASSWORD", settings.Secret("HOUSE_PLANNER_PASSWORD"))
            .WithEnvironment("HOUSE_SETUP_KEY", settings.SmartHome.Value("HOUSE_PLANNER_SETUP_KEY"))
            .WithEnvironment("HOUSE_DEMO", settings.SmartHome.Value("HOUSE_PLANNER_DEMO"))
            .WithBindMount(settings.SmartHome.DataPath("HOUSE_PLANNER_DATA", "../services-data/system/house-planner"), "/var/lib/house-planner")
            .WithLabPort(8080, "house-planner")
            .WithHomepage("Household", "Floor plan", "mdi-home-floor-a", "House planner", $"https://floorplan.{settings.Shared.DuckDnsDomain}");

        var worldmonitorRedis = builder.AddContainer("worldmonitor-redis", "redis", "7-alpine")
            .WithContainerName("worldmonitor-redis")
            .WithArgs("redis-server", "--requirepass", settings.Secret("WORLDMONITOR_REDIS_PASSWORD"), "--maxmemory", "256mb", "--maxmemory-policy", "allkeys-lru")
            .WithBindMount(settings.SmartHome.DataPath("WORLDMONITOR_REDIS_DATA", "../services-data/system/worldmonitor/redis"), "/data");

        var worldmonitorRedisRest = builder.AddContainer("worldmonitor-redis-rest", "worldmonitor-redis-rest", "local")
            .WithContainerName("worldmonitor-redis-rest")
            .WithEnvironment("SRH_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("SRH_CONNECTION_STRING", ReferenceExpression.Create($"redis://:{settings.Secret("WORLDMONITOR_REDIS_PASSWORD")}@worldmonitor-redis:6379"))
            .WaitFor(worldmonitorRedis);

        var worldmonitorRelay = builder.AddContainer("worldmonitor-ais-relay", "worldmonitor-ais-relay", "local")
            .WithContainerName("worldmonitor-ais-relay")
            .WithEnvironment("RELAY_SHARED_SECRET", settings.Secret("WORLDMONITOR_RELAY_SHARED_SECRET"))
            .WithEnvironment("UPSTASH_REDIS_REST_URL", "http://worldmonitor-redis-rest:80")
            .WithEnvironment("UPSTASH_REDIS_REST_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("UPSTASH_ALLOW_INSECURE_HTTP", "true")
            .WithEnvironment("API_BASE_URL", "http://worldmonitor:8080")
            .WithEnvironment("PORT", "3004")
            .WaitFor(worldmonitorRedisRest);

        _ = builder.AddContainer("worldmonitor", "worldmonitor", "local")
            .WithContainerName("worldmonitor")
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

        _ = builder.AddContainer("ntfy", "binwiederhier/ntfy", "latest")
            .WithContainerName("notifications")
            .WithDefaultMode(LabMode.Auto)
            .WithArgs("serve")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("NTFY_BASE_URL", $"https://notifications.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("NTFY_AUTH_DEFAULT_ACCESS", "read-write")
            .WithEnvironment("NTFY_ENABLE_LOGIN", "true")
            .WithEnvironment("NTFY_BEHIND_PROXY", "true")
            .WithBindMount(settings.SmartHome.DataPath("NTFY_AUTH", "../services-data/system/ntfy/auth"), "/var/lib/ntfy")
            .WithBindMount(settings.SmartHome.DataPath("NTFY_CACHE", "../services-data/system/ntfy/cache"), "/var/cache/ntfy")
            .WithConfigFile("configs/ntfy/server.yml", "/etc/ntfy/server.yml")
            .WithLabPort(80, "ntfy")
            .WithHomepage("Infrastructure", "Notifications", "mdi-bell-ring", href: $"https://notifications.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("ntfy", "http://ntfy:80",
                username: HomepageVar("NTFY_WIDGET_USERNAME"),
                password: HomepageVar("NTFY_WIDGET_PASSWORD"))
            .WithLab(lab => lab.Labels["homepage.widget.topic"] = settings.SmartHome.Value("NTFY_WIDGET_TOPIC", "general"));

        _ = builder.AddContainer("nextcloud", "nextcloud", "stable")
            .WithContainerName("nextcloud")
            .WithEnvironment("POSTGRES_HOST", "nextcloud-db")
            .WithEnvironment("POSTGRES_DB", "nextcloud")
            .WithEnvironment("POSTGRES_USER", "nextcloud")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("NEXTCLOUD_DB_PASSWORD"))
            .WithEnvironment("REDIS_HOST", "nextcloud-redis")
            .WithEnvironment("NEXTCLOUD_ADMIN_USER", settings.SmartHome.Value("NEXTCLOUD_ADMIN_USER", "admin"))
            .WithEnvironment("NEXTCLOUD_ADMIN_PASSWORD", settings.Secret("NEXTCLOUD_ADMIN_PASSWORD"))
            .WithEnvironment("NEXTCLOUD_TRUSTED_DOMAINS", $"nextcloud.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("OVERWRITEPROTOCOL", "https")
            .WithEnvironment("OVERWRITEHOST", $"nextcloud.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TRUSTED_PROXIES", "caddy")
            .WithBindMount(settings.SmartHome.DataPath("NEXTCLOUD_DATA", "../services-data/system/nextcloud/html"), "/var/www/html")
            .WithLabPort(80, "nextcloud")
            .WaitFor(dependencies["nextcloud-db"])
            .WaitFor(dependencies["nextcloud-redis"])
            .WithHomepage("Household", "Nextcloud", "si-nextcloud", "Files, calendar, contacts", $"https://nextcloud.{settings.Shared.DuckDnsDomain}");
    }
}
