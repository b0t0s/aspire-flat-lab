namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> NextcloudDb => Once(() => Postgres("nextcloud-db", "16-alpine", "nextcloud", "nextcloud", "NEXTCLOUD_DB_PASSWORD",
        Settings.SmartHome.DataPath("NEXTCLOUD_DB_DATA", "../services-data/system/nextcloud/postgres")));

    public IResourceBuilder<ContainerResource> NextcloudRedis => Once(() => Redis("nextcloud-redis"));

    public IResourceBuilder<ContainerResource> WorldmonitorRedis => Once(() => Builder.AddContainer("worldmonitor-redis", "redis", "7-alpine")
        .WithContainerName("worldmonitor-redis")
        .WithArgs("redis-server", "--requirepass", Settings.Secret("WORLDMONITOR_REDIS_PASSWORD"), "--maxmemory", "256mb", "--maxmemory-policy", "allkeys-lru")
        .WithBindMount(Settings.SmartHome.DataPath("WORLDMONITOR_REDIS_DATA", "../services-data/system/worldmonitor/redis"), "/data"));

    public IResourceBuilder<ContainerResource> WorldmonitorRedisRest => Once(() => Builder.AddContainer("worldmonitor-redis-rest", "worldmonitor-redis-rest", "local")
        .WithContainerName("worldmonitor-redis-rest")
        .WithEnvironment("SRH_TOKEN", Settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
        .WithEnvironment("SRH_CONNECTION_STRING", ReferenceExpression.Create($"redis://:{Settings.Secret("WORLDMONITOR_REDIS_PASSWORD")}@worldmonitor-redis:6379"))
        .WaitFor(WorldmonitorRedis));

    public IResourceBuilder<ContainerResource> WorldmonitorAisRelay => Once(() => Builder.AddContainer("worldmonitor-ais-relay", "worldmonitor-ais-relay", "local")
        .WithContainerName("worldmonitor-ais-relay")
        .WithEnvironment("RELAY_SHARED_SECRET", Settings.Secret("WORLDMONITOR_RELAY_SHARED_SECRET"))
        .WithEnvironment("UPSTASH_REDIS_REST_URL", "http://worldmonitor-redis-rest:80")
        .WithEnvironment("UPSTASH_REDIS_REST_TOKEN", Settings.Secret("WORLDMONITOR_REDIS_TOKEN"))
        .WithEnvironment("UPSTASH_ALLOW_INSECURE_HTTP", "true")
        .WithEnvironment("API_BASE_URL", "http://worldmonitor:8080")
        .WithEnvironment("PORT", "3004")
        .WaitFor(WorldmonitorRedisRest));
}
