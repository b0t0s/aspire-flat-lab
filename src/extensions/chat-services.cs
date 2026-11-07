using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Messaging" - host ports 17xxx; IRC/XMPP/Matrix-federation/TURN keep their standard ports (docs/ports.md).</summary>
internal static class ChatServices
{
    public static void AddChatServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
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
            .WithDefaultMode(LabMode.Auto)
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

        _ = builder.AddContainer("pingvin-share", "stonith404/pingvin-share", "latest")
            .WithContainerName("sharing")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Chat.DataPath("PINGVIN_DATA", "../services-data/system/pingvin-share"), "/app/backend/pingvin-share-data")
            .WithBindMount(Path.Combine(settings.Chat.DataPath("PINGVIN_DATA", "../services-data/system/pingvin-share"), "data"), "/opt/app/backend/data")
            .WithLabPort(3000, "pingvin-share")
            .WithHomepage("Messaging", "Sharing", "mdi-share-variant", href: $"https://sharing.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("smp-server", "simplexchat/smp-server", "latest")
            .WithContainerName("smp-server")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("ADDR", settings.Chat.Value("SMP_SERVER_ADDR", settings.Shared.DuckDnsDomain))
            .WithEnvironment("PASS", settings.Secret("SMP_SERVER_PASS", SecretKind.Manual))
            .WithBindMount(settings.Chat.DataPath("SMP_SERVER_CONFIG", "../services-data/system/smp-server/config"), "/etc/opt/simplex")
            .WithBindMount(settings.Chat.DataPath("SMP_SERVER_LOGS", "../services-data/system/smp-server/logs"), "/var/opt/simplex")
            .WithPublishedPort(5223, 5223)
            .WithHomepage("Messaging", "SimpleX relay", "mdi-lock", href: $"https://simplex.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("continuwuity", "forgejo.ellis.link/continuwuation/continuwuity", "latest")
            .WithContainerName("matrix-server")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("CONTINUWUITY_SERVER_NAME", settings.Shared.DuckDnsDomain)
            .WithEnvironment("CONTINUWUITY_DATABASE_PATH", "/var/lib/continuwuity")
            .WithEnvironment("CONTINUWUITY_ADDRESS", "0.0.0.0")
            .WithEnvironment("CONTINUWUITY_PORT", "8008")
            .WithBindMount(settings.Chat.DataPath("CONTINUWUITY_DATA", "../services-data/system/continuwuity"), "/var/lib/continuwuity")
            .WithLabPort(8008, "continuwuity")
            .WithHomepage("Messaging", "Matrix", "mdi-server-network", "", $"https://matrix.{settings.Shared.DuckDnsDomain}");

        // IRC
        var ergochat = builder.AddContainer("ergochat", "ghcr.io/ergochat/ergo", "latest")
            .WithContainerName("irc-server")
            .WithDefaultMode(LabMode.Manual)
            .WithEndpoint(6667, 6667, name: "irc")
            .WithLabPort(8080, "irc-web")
            .WithHomepage("Messaging", "IRC", "mdi-server-network", "SERVER", $"https://irc.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("thelounge", "ghcr.io/thelounge/thelounge", "latest")
            .WithContainerName("irc")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("HOST", "0.0.0.0")
            .WithEnvironment("PORT", "9000")
            .WithBindMount(settings.Chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "/var/opt/thelounge")
            .WithConfigFile("configs/thelounge/config.js", "/var/opt/thelounge/config.js", Path.Combine(settings.Chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "config.js"))
            .WithLabPort(9000, "thelounge")
            .WaitFor(ergochat)
            .WithHomepage("Messaging", "IRC", "mdi-chat", href: $"https://irc.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("stalwart", "stalwartlabs/stalwart", "v1")
            .WithContainerName("mail")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("STALWART_RECOVERY_ADMIN", ReferenceExpression.Create($"admin:{settings.Secret("STALWART_ADMIN_PASSWORD")}"))
            .WithBindMount(settings.Chat.DataPath("STALWART_DATA", "../services-data/system/stalwart/data"), "/var/lib/stalwart")
            .WithBindMount(settings.Chat.DataPath("STALWART_CONFIG", "../services-data/system/stalwart/config"), "/etc/stalwart")
            .WithEndpoint(25, 25, name: "smtp")
            .WithEndpoint(465, 465, name: "smtps")
            .WithEndpoint(993, 993, name: "imaps")
            .WithLabPort(8080, "stalwart-web")
            .WithHomepage("Messaging", "Mail", "mdi-email", "Stalwart mail server", $"https://mail.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("email-signature", "ghcr.io/clarkemedia/email-signature-generator", "latest")
            .WithContainerName("email-signature-generator")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(80, "email-signature")
            .WithHomepage("Messaging", "Email Signature Generator", "mdi-email-edit", "Email signature generator", $"https://email-signature-generator.{settings.Shared.DuckDnsDomain}");
    }
}
