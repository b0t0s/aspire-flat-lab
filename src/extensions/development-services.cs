internal static class DevelopmentServices
{
    public static void AddDevelopmentServices(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("onedev", "1dev/server", "latest")
            .WithContainerName("projects")
            .WithStartPolicy(true)
            .WithBindMount(settings.Development.DataPath("ONEDEV_DATA", "../services-data/system/onedev"), "/opt/onedev")
            .WithDockerSocket(isReadOnly: false)
            .WithLabPort(6610, "onedev-http")
            .WithLabPort(6611, "onedev-agent")
            .WithHomepage("Development", "Projects", "mdi-git", href: $"https://projects.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("opengist", "ghcr.io/thomiceli/opengist", "latest")
            .WithContainerName("snippets")
            .WithStartPolicy(true)
            .WithEnvironment("OG_LISTEN_HOST", "0.0.0.0")
            .WithEnvironment("OG_LISTEN_PORT", "6157")
            .WithEnvironment("OG_REPOSITORY_PATH", "/app/repositories")
            .WithEnvironment("OG_SECRET_KEY", settings.Secret("OPENGIST_SECRET"))
            .WithBindMount(settings.Development.DataPath("OPENGIST_DATA", "../services-data/system/opengist"), "/opengist")
            .WithBindMount(settings.Development.DataPath("OPENGIST_REPOS", "../services-data/system/opengist-repos"), "/app/repositories")
            .WithLabPort(6157, "opengist")
            .WithHomepage("Development", "Snippets", "mdi-source-repository", href: $"https://snippets.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("it-tools", "corentinth/it-tools", "latest")
            .WithContainerName("toolbox")
            .WithStartPolicy(true)
            .WithLabPort(80, "it-tools")
            .WithHomepage("Development", "Toolbox", "mdi-tools", href: $"https://tools.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("convertx", "ghcr.io/c4illin/convertx", "latest")
            .WithContainerName("converter")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithEnvironment("JWT_SECRET", settings.Secret("CONVERTX_JWT_SECRET"))
            .WithBindMount(settings.Development.DataPath("CONVERTX_DATA", "../services-data/system/convertx"), "/app/data")
            .WithLabPort(3000, "convertx")
            .WithHomepage("Development", "Converter", "mdi-file-convert", href: $"https://convert.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("excalidraw", "excalidraw/excalidraw", "latest")
            .WithContainerName("whiteboard")
            .WithStartPolicy(settings.Development.StartByDefault)
            .WithLabPort(80, "excalidraw")
            .WithHomepage("Development", "Whiteboard", "mdi-draw", href: $"https://whiteboard.{settings.Shared.DuckDnsDomain}");
    }
}
