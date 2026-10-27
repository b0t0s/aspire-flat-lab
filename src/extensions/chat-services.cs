using apphost.cs.src;

/// <summary>Homepage group "Messaging" - host ports 17xxx; IRC/XMPP/Matrix-federation/TURN keep their standard ports (docs/ports.md).</summary>
internal static class ChatServices
{
    public static void AddChatServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var hermesDataPath = settings.Chat.DataPath("HERMES_DATA_PATH", "../services-data/system/hermes");
        var hermesWorkspacePath = settings.Chat.DataPath("HERMES_WORKSPACE_PATH", "../services-data/public/agents_workspace");
        var hermesPublicPath = settings.Chat.DataPath("HERMES_PUBLIC_PATH", "../services-data/public");

        //   First start:
        //   docker run -it --rm \
        //   -v ../services-data/system/hermes:/opt/data \
        //   nousresearch/hermes-agent setup
        _ = builder.AddContainer("hermes", "nousresearch/hermes-agent", "latest")
            .WithContainerName("hermes")
            .WithStartPolicy(true)
            .WithArgs("gateway", "run")
            .WithEnvironment("HERMES_DASHBOARD", "1")
            .WithEnvironment("HERMES_DASHBOARD_HOST", "0.0.0.0")
            .WithEnvironment("HERMES_DASHBOARD_PUBLIC_URL", $"https://hermes.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HERMES_DASHBOARD_BASIC_AUTH_USERNAME", settings.Chat.Value("HERMES_DASHBOARD_BASIC_AUTH_USERNAME"))
            .WithEnvironment("HERMES_DASHBOARD_BASIC_AUTH_PASSWORD", settings.Chat.Value("HERMES_DASHBOARD_BASIC_AUTH_PASSWORD"))
            .WithEnvironment("HERMES_DASHBOARD_BASIC_AUTH_SECRET", settings.Chat.Value("HERMES_DASHBOARD_BASIC_AUTH_SECRET"))
            .WithEnvironment("HERMES_HOME_MODE", "0700")
            .WithEnvironment("HERMES_UID", settings.Shared.Uid)
            .WithEnvironment("HERMES_GID", settings.Chat.Value("DOCKER_GID", settings.Shared.Gid))
            .WithEnvironment("HOMEPAGE_MCP_TOKEN", settings.Secret("HOMEPAGE_MCP_TOKEN"))
            .WithDockerSocket()
            .WithGroupAdd(settings.Chat.Value("DOCKER_GID", "124"))
            .WithEnvironment("TERMINAL_DOCKER_RUN_AS_HOST_USER", "true")
            .WithEnvironment("TERMINAL_DOCKER_VOLUMES", System.Text.Json.JsonSerializer.Serialize(new[]
            {
                $"{hermesWorkspacePath}:/workspace",
                $"{hermesPublicPath}:/public:ro",
            }))
            .WithBindMount(hermesDataPath, "/opt/data")
            .WithBindMount(hermesWorkspacePath, "/workspace")
            .WithBindMount(hermesPublicPath, "/public", isReadOnly: true)
            .WithHomepage("Messaging", "Agent", "mdi-robot", "", $"https://hermes.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("open-webui", "ghcr.io/open-webui/open-webui", "main")
            .WithContainerName("llm")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("ENABLE_SIGNUP", "True")
            .WithEnvironment("OLLAMA_BASE_URL", "http://ollama:11434")
            .WithBindMount(settings.Chat.DataPath("OPEN_WEBUI_DATA", "../services-data/system/open-webui"), "/app/backend/data")
            .WithEndpoint(10007, 8080, name: "open-webui")
            .WaitFor(dependencies["ollama"])
            .WithHomepage("Development", "LLM", "mdi-robot", href: $"https://llm.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("pingvin-share", "stonith404/pingvin-share", "latest")
            .WithContainerName("sharing")
            .WithStartPolicy(true)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Chat.DataPath("PINGVIN_DATA", "../services-data/system/pingvin-share"), "/app/backend/pingvin-share-data")
            .WithBindMount(Path.Combine(settings.Chat.DataPath("PINGVIN_DATA", "../services-data/system/pingvin-share"), "data"), "/opt/app/backend/data")
            .WithLabPort(3000, "pingvin-share")
            .WithHomepage("Messaging", "Sharing", "mdi-share-variant", href: $"https://sharing.{settings.Shared.DuckDnsDomain}");

        // IRC
        var ergochat = builder.AddContainer("irc-ergochat", "ghcr.io/ergochat/ergo", "latest")
            .WithContainerName("irc-server")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithEndpoint(6667, 6667, name: "irc")
            .WithLabPort(8080, "irc-web")
            .WithHomepage("Messaging", "IRC", "mdi-server-network", "SERVER", $"https://irc.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("irc-thelounge", "ghcr.io/thelounge/thelounge", "latest")
            .WithContainerName("irc")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithEnvironment("HOST", "0.0.0.0")
            .WithEnvironment("PORT", "9000")
            .WithBindMount(settings.Chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "/var/opt/thelounge")
            .WithConfigFile("configs/thelounge/config.js", "/var/opt/thelounge/config.js", Path.Combine(settings.Chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "config.js"))
            .WithLabPort(9000, "thelounge")
            .WaitFor(ergochat)
            .WithHomepage("Messaging", "IRC", "mdi-chat", href: $"https://irc.{settings.Shared.DuckDnsDomain}");
    }
}
