using apphost.cs.src;
using static EnvironmentSettings;

internal static class SonarServices
{
    public static void AddSonarServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var sonarAppPath = settings.Chat.Value("SONAR_APP_PATH", "../additional-software/sonar/app");

        _ = builder.AddContainer("livekit", "livekit/livekit-server", "latest")
            .WithContainerName("livekit")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithArgs("--config", "/etc/livekit/livekit.yaml")
            .WithConfigTemplate("configs/livekit/livekit.yaml.template", "/etc/livekit", "livekit.yaml", new Dictionary<string, object>
            {
                ["DOMAIN"] = settings.Shared.DuckDnsDomain,
                ["API_KEY"] = settings.Secret("LIVEKIT_API_KEY"),
                ["API_SECRET"] = settings.Secret("LIVEKIT_API_SECRET", SecretKind.LongPassword),
                ["NODE_IP"] = settings.Shared.NodeIp,
            })
            .WithLabPort(7880, "livekit-http")
            .WithEndpoint(7881, 7881, name: "livekit-rtc-tcp")
            .WithPublishedPort(7882, 7882, "udp")
            .WithPublishedPort(3478, 3478, "udp")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WaitFor(dependencies["livekit-redis"])
            .WithHomepage("Messaging", "Livekit", "mdi-server-network", href: $"https://livekit.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("lab-mint", "golang", "1.26-alpine")
            .WithContainerName("lab-mint")
            .WithStartPolicy(settings.Chat.StartByDefault)
            .WithArgs("go", "run", "/app/main.go")
            .WithBindMount(Path.GetFullPath("../additional-software/sonar/infra/lab-mint/main.go", builder.AppHostDirectory), "/app/main.go", isReadOnly: true)
            .WithEnvironment("LIVEKIT_API_KEY", settings.Secret("LIVEKIT_API_KEY"))
            .WithEnvironment("LIVEKIT_API_SECRET", settings.Secret("LIVEKIT_API_SECRET", SecretKind.LongPassword))
            .WithEnvironment("LIVEKIT_WS_URL", $"wss://livekit.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("PORT", "8080")
            .WithHealthcheck("wget -q -O /dev/null http://127.0.0.1:8080/healthz", startPeriod: "30s");

        _ = builder.AddViteApp("chat-sonar-app", Path.GetFullPath(sonarAppPath, builder.AppHostDirectory))
            .WithEnvironment("VITE_LIVEKIT_WS_URL", $"wss://livekit.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("VITE_MINT_URL", settings.Chat.Value("SONAR_MINT_URL", $"https://mint.{settings.Shared.DuckDnsDomain}/mint"))
            .WithEnvironment("VITE_RELEASES_URL", settings.Chat.Value("SONAR_RELEASES_URL", "https://releases.sonar.app"))
            .WithHttpEndpoint(env: "VITE_PORT", port: 5173, name: "vite-dev")
            .WithExternalHttpEndpoints();
    }
}
