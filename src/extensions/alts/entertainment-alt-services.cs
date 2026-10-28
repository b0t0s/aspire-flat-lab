using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Entertainment" - host ports 14xxx (docs/ports.md).</summary>
internal static class EntertainmentAltServices
{
    public static void AddAltEntertainmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("dm-hero", "ghcr.io/flo0806/dm-hero", "latest")
            .WithContainerName("dm-hero")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("NITRO_PORT", "3000")
            .WithEnvironment("NITRO_HOST", "0.0.0.0")
            .WithBindMount(settings.Entertainment.DataPath("DM_HERO_DATA", "../services-data/system/dm-hero/data"), "/app/data")
            .WithBindMount(settings.Entertainment.DataPath("DM_HERO_UPLOADS", "../services-data/system/dm-hero/uploads"), "/app/.output/public/uploads")
            .WithLabPort(3000, "dm-hero")
            .WithHomepage("Entertainment", "DM Hero", "mdi-dice-d20", "D&D campaign manager", $"https://dm-hero.{settings.Shared.DuckDnsDomain}");
    }
}
