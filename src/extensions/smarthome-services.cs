using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder,  LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("ntfy", "binwiederhier/ntfy", "latest")
            .WithContainerName("notifications")
            .WithDefaultMode(LabMode.Manual)
            .WithArgs("serve")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("NTFY_BASE_URL", $"https://notifications.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("NTFY_AUTH_DEFAULT_ACCESS", "read-write")
            .WithEnvironment("NTFY_ENABLE_LOGIN", "true")
            .WithEnvironment("NTFY_ENABLE_SIGNUP", "true")
            .WithEnvironment("NTFY_BEHIND_PROXY", "true")
            .WithBindMount(settings.SmartHome.DataPath("NTFY_AUTH", "../services-data/system/ntfy/auth"), "/var/lib/ntfy")
            .WithBindMount(settings.SmartHome.DataPath("NTFY_CACHE", "../services-data/system/ntfy/cache"), "/var/cache/ntfy")
            .WithConfigFile("configs/ntfy/server.yml", "/etc/ntfy/server.yml")
            .WithLabPort(80, "ntfy")
            .WithHomepage("Infrastructure", "Notifications", "mdi-bell-ring", "Push notifications", href: $"https://notifications.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("ntfy", "http://ntfy:80",
                username: HomepageVar("NTFY_WIDGET_USERNAME"),
                password: HomepageVar("NTFY_WIDGET_PASSWORD"))
            .WithLab(lab => lab.Labels["homepage.widget.topic"] = settings.SmartHome.Value("NTFY_WIDGET_TOPIC", "general"));

        _ = builder.AddContainer("worldmonitor", "worldmonitor", "local")
            .WithContainerName("world")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("UPSTASH_REDIS_REST_URL", "http://worldmonitor-redis-rest:80")
            .WithEnvironment("UPSTASH_REDIS_REST_TOKEN", settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
            .WithEnvironment("WM_SESSION_SECRET", settings.Secret("WORLDMONITOR_SESSION_SECRET", SecretKind.Hex32))
            .WithEnvironment("LOCAL_API_PORT", "46123")
            .WithEnvironment("LOCAL_API_MODE", "docker")
            .WithEnvironment("LOCAL_API_CLOUD_FALLBACK", "false")
            .WithEnvironment("WS_RELAY_URL", "http://worldmonitor-ais-relay:3004")
            .WithEnvironment("RELAY_SHARED_SECRET", settings.Secret("WORLDMONITOR_RELAY_SHARED_SECRET"))
            .WithLabPort(8080, "worldmonitor")
            .WaitFor(dependencies.WorldmonitorRedisRest)
            .WaitFor(dependencies.WorldmonitorAisRelay)
            .WithHomepage("Entertainment", "WorldMonitor", "mdi-earth-box", "Global intelligence dashboard", $"https://world.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("nextcloud", "nextcloud", "stable")
            .WithContainerName("drive")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("POSTGRES_HOST", "nextcloud-db")
            .WithEnvironment("POSTGRES_DB", "drive")
            .WithEnvironment("POSTGRES_USER", "drive")
            .WithEnvironment("POSTGRES_PASSWORD", settings.Secret("NEXTCLOUD_DB_PASSWORD"))
            .WithEnvironment("REDIS_HOST", "nextcloud-redis")
            .WithEnvironment("NEXTCLOUD_ADMIN_USER", settings.SmartHome.Value("NEXTCLOUD_ADMIN_USER", "admin"))
            .WithEnvironment("NEXTCLOUD_ADMIN_PASSWORD", settings.Secret("NEXTCLOUD_ADMIN_PASSWORD"))
            .WithEnvironment("NEXTCLOUD_TRUSTED_DOMAINS", $"drive.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("OVERWRITEPROTOCOL", "https")
            .WithEnvironment("OVERWRITEHOST", $"drive.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TRUSTED_PROXIES", "caddy")
            .WithBindMount(settings.SmartHome.DataPath("NEXTCLOUD_DATA", "../services-data/system/nextcloud/html"), "/var/www/html")
            .WithLabPort(80, "nextcloud")
            .WaitFor(dependencies.NextcloudDb)
            .WaitFor(dependencies.NextcloudRedis)
            .WithHomepage("Household", "Nextcloud", "si-nextcloud", "Files, calendar, contacts", $"https://drive.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("house-planner", builder.RepoPath("src/containers/house-planner"))
            .WithContainerName("floorplan")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("HOUSE_PASSWORD", settings.Secret("HOUSE_PLANNER_PASSWORD"))
            .WithEnvironment("HOUSE_SETUP_KEY", settings.SmartHome.Value("HOUSE_PLANNER_SETUP_KEY"))
            .WithEnvironment("HOUSE_DEMO", settings.SmartHome.Value("HOUSE_PLANNER_DEMO"))
            .WithBindMount(settings.SmartHome.DataPath("HOUSE_PLANNER_DATA", "../services-data/system/house-planner"), "/var/lib/house-planner")
            .WithLabPort(8080, "house-planner")
            .WithHomepage("Household", "Floor plan", "mdi-home-floor-a", "House planner", $"https://floorplan.{settings.Shared.DuckDnsDomain}");
    }
}
