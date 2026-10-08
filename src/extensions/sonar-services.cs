public static class SonarServices
{
    public static void AddSonarServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var chat = settings.Chat;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("livekit", "livekit/livekit-server", "latest")
            .WithContainerName("livekit")
            .WithStartPolicy(chat.StartByDefault)
            .WithArgs("--config", "/etc/livekit/livekit.yaml")
            .WithConfigTemplate("configs/livekit/livekit.yaml.template", "/etc/livekit", "livekit.yaml", new Dictionary<string, object>
            {
                ["DOMAIN"] = domain,
                ["API_KEY"] = settings.Secret("LIVEKIT_API_KEY"),
                ["API_SECRET"] = settings.Secret("LIVEKIT_API_SECRET", SecretKind.LongPassword),
                ["NODE_IP"] = shared.NodeIp,
            })
            .WithLabPort(7880, "livekit-http")
            .WithEndpoint(7881, 7881, name: "livekit-rtc-tcp")
            .WithPublishedPort(7882, 7882, "udp")
            .WithPublishedPort(3478, 3478, "udp")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WaitFor(dependencies["livekit-redis"]);

        // var distPath = chat.Value("SONAR_DIST_PATH", "../additional-software/sonar/app/dist");
        // builder.AddContainer("sonar", "nginx:alpine", "latest")
        //     .WithContainerName("sonar")
        //     .WithStartPolicy(chat.StartByDefault)
        //     .WithBindMount(Path.GetFullPath(distPath, builder.AppHostDirectory), "/usr/share/nginx/html")
        //     .WithEnvironment("TZ", shared.TimeZone)
        //     .WithLabPort(80, "sonar-http")
        //     .WithHomepage("Messaging", "SONAR", "mdi-account-group", href: $"https://sonar.{domain}");

        // Build the Go binary on the deploy host with:
        //   cd additional-software/sonar/infra/lab-mint
        //   CGO_ENABLED=0 GOOS=linux GOARCH=arm64 \
        //     go build -trimpath -ldflags="-s -w" -o dist/lab-mint main.go
        var mintPath = chat.Value("SONAR_MINT_PATH", "../additional-software/sonar/infra/lab-mint/dist/lab-mint");
        builder.AddContainer("sonar-mint", "gcr.io/distroless/static-debian12:nonroot", "latest")
            .WithContainerName("sonar-mint")
            .WithStartPolicy(chat.StartByDefault)
            .WithBindMount(Path.GetFullPath(mintPath, builder.AppHostDirectory), "/usr/local/bin/lab-mint")
            .WithEnvironment("LIVEKIT_API_KEY", chat.Value("LIVEKIT_API_KEY", ""))
            .WithEnvironment("LIVEKIT_API_SECRET", chat.Value("LIVEKIT_API_SECRET", ""))
            .WithEnvironment("LIVEKIT_WS_URL", chat.Value("LIVEKIT_WS_URL", $"wss://livekit.{domain}"))
            .WithEnvironment("PORT", "8080")
            .WithEnvironment("TZ", shared.TimeZone)
            .WithLabPort(8080, "sonar-mint-internal")
            .WithArgs("/usr/local/bin/lab-mint")
            .WithHomepage("Messaging", "SONAR Mint", "mdi-key-variant", href: $"https://mint.{domain}/healthz");

        // builder.AddContainer("chat-sonar-mgmt", "nginx:alpine", "latest")
        //     .WithContainerName("sonar-mgmt")
        //     .WithStartPolicy(chat.StartByDefault)
        //     .WithBindMount(Path.GetFullPath(distPath, builder.AppHostDirectory), "/usr/share/nginx/html:ro")
        //     .WithLabPort(8081, "sonar-mgmt-http")
        //     .WithHomepage("Messaging", "SONAR Mgmt", "mdi-cog", href: $"https://sonar-mgmt.{domain}");
    }
}
