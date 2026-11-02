using apphost.cs.src;
using static EnvironmentSettings;

internal static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
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
    }
}
