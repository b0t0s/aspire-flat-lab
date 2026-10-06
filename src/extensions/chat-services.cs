using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Messaging" - host ports 17xxx; IRC/XMPP/Matrix-federation/TURN keep their standard ports (docs/ports.md).</summary>
public static class ChatServices
{
    public static void AddChatServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var chat = settings.Chat;
        var variants = settings.ChatVariants;
        var tools = settings.Tools;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("pingvin-share", "stonith404/pingvin-share", "latest")
            .WithContainerName("sharing")
            .WithEnvironment("TZ", shared.TimeZone)
            .WithBindMount(tools.DataPath("PINGVIN_DATA", "../services-data/system/pingvin-share"), "/app/backend/pingvin-share-data")
            .WithLabPort(3000, "pingvin-share")
            .WithHomepage("Messaging", "Sharing", "mdi-share-variant", href: $"https://sharing.{domain}");

        // IRC
        var ergochat = builder.AddContainer("chat-irc-ergochat", "ghcr.io/ergochat/ergo", "latest")
            .WithContainerName("irc-server")
            .WithEndpoint(6667, 6667, name: "irc")
            .WithLabPort(8080, "irc-web")
            .WithHomepage("Messaging", "IRC", "mdi-server-network", "SERVER", $"https://irc.{domain}");

        builder.AddContainer("chat-irc-thelounge", "ghcr.io/thelounge/thelounge", "latest")
            .WithContainerName("irc")
            .WithEnvironment("HOST", "0.0.0.0")
            .WithEnvironment("PORT", "9000")
            .WithBindMount(chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "/var/opt/thelounge")
            .WithConfigFile("configs/thelounge/config.js", "/var/opt/thelounge/config.js",
                Path.Combine(chat.DataPath("THELOUNGE_DATA", "../services-data/system/thelounge"), "config.js"))
            .WithLabPort(9000, "thelounge")
            .WaitFor(ergochat)
            .WithHomepage("Messaging", "IRC", "mdi-chat", href: $"https://irc.{domain}");

        // Matrix
        builder.AddContainer("element-web", "vectorim/element-web", "latest")
            .WithContainerName("matrix")
            .WithStartPolicy(chat.StartByDefault)
            .WithConfigTemplate("configs/element/config.json.template", "/app", "config.json", new Dictionary<string, object> { ["DOMAIN"] = domain })
            .WithLabPort(80, "element-web")
            .WithHomepage("Messaging", "Matrix", "mdi-chat", href: $"https://matrix.{domain}");

        AddSynapse(builder, dependencies, settings);

        // XMPP
        builder.AddContainer("snikket-portal", "snikket/snikket-web-portal", "dev")
            .WithContainerName("snikket-portal")
            .WithStartPolicy(variants.StartByDefault)
            .WithSnikketSettings(settings)
            .WithHomepage("Messaging", "Portal", "mdi-chat", href: $"https://chat.{domain}");

        builder.AddDockerfile("conversejs", builder.RepoPath("src/containers/xmpp-web"), "Dockerfile")
            .WithContainerName("xmpp")
            .WithStartPolicy(chat.StartByDefault)
            .WithEnvironment("XMPP_DOMAIN", domain)
            .WithEnvironment("XMPP_WS_URL", chat.Value("XMPP_WS_URL", $"wss://xmpp.{domain}/ws"))
            .WithEnvironment("XMPP_BOSH_URL", chat.Value("XMPP_BOSH_URL", $"https://xmpp.{domain}/bosh"))
            .WithLabPort(80, "conversejs")
            .WithHomepage("Messaging", "XMPP", "mdi-chat", href: $"https://xmpp.{domain}");

        builder.AddContainer("chat-xmpp-ejabberd", "ghcr.io/processone/ejabberd", "latest")
            .WithContainerName("xmpp-server")
            .WithStartPolicy(variants.StartByDefault)
            .WithEnvironment("EJABBERD_MACRO_HOST", domain)
            .WithEnvironment("EJABBERD_MACRO_ADMIN", $"admin@{domain}")
            .WithEnvironment("REGISTER_ADMIN_PASSWORD", settings.Secret("EJABBERD_ADMIN_PASSWORD"))
            .WithEnvironment("XMPP_DOMAIN", domain)
            .WithBindMount(chat.DataPath("EJABBERD_DATA", "../services-data/system/ejabberd"), "/opt/ejabberd/database")
            .WithConfigFile("configs/ejabberd/ejabberd.yml", "/opt/ejabberd/conf/ejabberd.yml",
                chat.DataPath("EJABBERD_CONFIG", "../services-data/system/ejabberd/conf/ejabberd.yml"))
            .WithEndpoint(5222, 5222, name: "xmpp-client")
            .WithEndpoint(5223, 5223, name: "xmpp-client-tls")
            .WithEndpoint(5269, 5269, name: "xmpp-s2s")
            .WithLabPort(5280, "xmpp-http")
            .WithLab(lab => lab.Hostname = $"xmpp.{domain}")
            .WithHomepage("Messaging", "XMPP", "mdi-server-network", "SERVER", $"https://xmpp.{domain}");

        // Lemmy (proxy/pictrs/postfix live in Dependencies.cs; hostnames match configs/lemmy/nginx_internal.conf)
        var lemmy = builder.AddContainer("lemmy", "dessalines/lemmy", "0.19.20")
            .WithContainerName("forum")
            .WithContainerNetworkAlias("lemmy")
            .WithStartPolicy(chat.StartByDefault)
            .WithEnvironment("LEMMY_DATABASE_URL", ReferenceExpression.Create($"postgres://lemmy:{settings.Secret("LEMMY_DB_PASSWORD")}@lemmy-db:5432/lemmy"))
            .WithEnvironment("LEMMY_HOSTNAME", $"forum.{domain}")
            .WithEnvironment("LEMMY_PORT", "8536")
            .WithEnvironment("LEMMY_CONFIG_LOCATION", "/config/config.hjson")
            .WithConfigTemplate("configs/lemmy/config.hjson.template", "/config", "config.hjson", new Dictionary<string, object>
            {
                ["HOSTNAME"] = $"forum.{domain}",
                ["DOMAIN"] = domain,
                ["PICTRS_API_KEY"] = settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"),
            }, escape: value => value.Replace("\\", "\\\\").Replace("\"", "\\\""))
            .WithLabPort(8536, "lemmy")
            .WaitFor(dependencies["lemmy-db"])
            .WithHomepage("Messaging", "Forum", "mdi-forum", "SERVER", $"https://forum.{domain}");

        var lemmyUi = builder.AddContainer("lemmy-project-ui", "dessalines/lemmy-ui", "0.19.20")
            .WithContainerName("forum-ui")
            .WithContainerNetworkAlias("lemmy-ui")
            .WithStartPolicy(chat.StartByDefault)
            .WithEnvironment("LEMMY_UI_LEMMY_INTERNAL_HOST", "forum:8536")
            .WithEnvironment("LEMMY_UI_LEMMY_EXTERNAL_HOST", $"forum.{domain}")
            .WithEnvironment("LEMMY_UI_HTTPS", "true")
            .WithBindMount(chat.DataPath("LEMMY_UI_THEMES", "../services-data/system/lemmy-project/lemmy-ui/extra_themes"), "/app/extra_themes")
            .WaitFor(lemmy)
            .WithHomepage("Messaging", "Forum", "mdi-forum", "Federated network", $"https://forum.{domain}");

        dependencies["lemmy-project-proxy"]
            .WaitFor(dependencies["lemmy-project-pictrs"])
            .WaitFor(lemmyUi);

        builder.AddContainer("livekit", "livekit/livekit-server", "latest")
            .WithContainerName("livekit")
            .WithStartPolicy(chat.StartByDefault)
            .WithArgs("--config", "/etc/livekit/livekit.yaml")
            .WithConfigTemplate("configs/livekit/livekit.yaml.template", "/etc/livekit", "livekit.yaml", new Dictionary<string, object>
            {
                ["DOMAIN"] = domain,
                ["API_KEY"] = settings.Secret("LIVEKIT_API_KEY"),
                ["API_SECRET"] = settings.Secret("LIVEKIT_API_SECRET", SecretKind.LongPassword),
            })
            .WithLabPort(7880, "livekit-http")
            .WithEndpoint(7881, 7881, name: "livekit-rtc-tcp")
            .WithPublishedPort(7882, 7882, "udp")
            .WithPublishedPort(3478, 3478, "udp")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WaitFor(dependencies["livekit-redis"]);
    }

    /// <summary>
    /// Synapse without an init container: homeserver.yaml and the log config are rendered from configs/synapse/*.template
    /// into /config; the signing key is created by Synapse itself (--generate-keys).
    /// </summary>
    private static void AddSynapse(IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        const int synapseUid = 991;
        var chat = settings.Chat;
        var domain = settings.Shared.DuckDnsDomain;
        var dataPath = chat.DataPath("SYNAPSE_DATA", "../services-data/system/synapse");
        var variables = new Dictionary<string, object>
        {
            ["SERVER_NAME"] = domain,
            ["DB_HOST"] = "synapse-db",
            ["DB_USER"] = "synapse",
            ["DB_NAME"] = "synapse",
            ["DB_PASSWORD"] = settings.Secret("SYNAPSE_DB_PASSWORD"),
            ["MACAROON_SECRET_KEY"] = settings.Secret("SYNAPSE_MACAROON_SECRET_KEY", SecretKind.Hex32, Path.Combine(dataPath, $"{domain}.macaroon.secret")),
        };
        // Values sit inside YAML single quotes in the templates.
        static string Yaml(string value) => value.Replace("'", "''");

        builder.AddContainer("synapse", "matrixdotorg/synapse", "v1.95.1")
            .WithContainerName("matrix-server")
            .WithStartPolicy(chat.StartByDefault)
            .WithEnvironment("SYNAPSE_CONFIG_PATH", "/config/homeserver.yaml")
            .WithConfigTemplate("configs/synapse/homeserver.yaml.template", "/config", "homeserver.yaml", variables, Yaml, synapseUid)
            .WithConfigTemplate("configs/synapse/synapse-log.config.template", "/config", "log.config", variables, Yaml, synapseUid)
            // /data also holds the synapse-db data dir (UID 70), so only the paths synapse owns are chowned.
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", string.Join(" && ",
                "python -m synapse.app.homeserver --config-path /config/homeserver.yaml --generate-keys",
                "mkdir -p /data/media_store /data/uploads",
                $"chown {synapseUid}:{synapseUid} /data /data/media_store /data/uploads /data/*.signing.key",
                "exec /start.py"))
            .WithBindMount(dataPath, "/data")
            .WithLabPort(8008, "synapse-client")
            .WithEndpoint(8448, 8448, name: "synapse-federation")
            .WaitFor(dependencies["synapse-db"])
            .WithHomepage("Messaging", "Matrix", "mdi-server-network", "SERVER");
    }

    /// <summary>Snikket reads its settings from env; they come from .env instead of a separate snikket.conf.</summary>
    public static IResourceBuilder<ContainerResource> WithSnikketSettings(this IResourceBuilder<ContainerResource> resource, EnvironmentSettings settings) =>
        resource
            .WithEnvironment("SNIKKET_DOMAIN", settings.ChatVariants.Value("SNIKKET_DOMAIN", $"chat.{settings.Shared.DuckDnsDomain}"))
            .WithEnvironment("SNIKKET_ADMIN_EMAIL", settings.ChatVariants.Value("SNIKKET_ADMIN_EMAIL", $"admin@{settings.Shared.DuckDnsDomain}"));
}
