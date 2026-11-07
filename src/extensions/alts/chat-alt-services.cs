using apphost.cs.src;
using static EnvironmentSettings;

internal static class ChatAltServices
{
    public static void AddChatAltServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("open-webui", "ghcr.io/open-webui/open-webui", "main")
            .WithContainerName("llm")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("ENABLE_SIGNUP", "True")
            .WithEnvironment("OLLAMA_BASE_URL", "http://ollama:11434")
            .WithBindMount(settings.Chat.DataPath("OPEN_WEBUI_DATA", "../services-data/system/open-webui"), "/app/backend/data")
            .WithEndpoint(10007, 8080, name: "open-webui")
            .WaitFor(dependencies["ollama"])
            .WithHomepage("Development", "LLM", "mdi-robot", href: $"https://llm.{settings.Shared.DuckDnsDomain}");

        // XMPP: Snikket (all-in-one) needs host networking for its own 80/443/5222/5269.
        var snikketDataPath = settings.Chat.DataPath("SNIKKET_DATA_PATH", "../services-data/system/snikket");
        var snikketAcmePath = Path.Combine(snikketDataPath, "acme-challenge");
        IResourceBuilder<ContainerResource> Snikket(string name, string image)
        {
            return builder.AddContainer(name, image, "dev")
                .WithContainerName(name)
                .WithHostNetwork()
                .WithSnikketSettings(settings)
                .WithBindMount(snikketDataPath, "/snikket");
        }

        _ = Snikket("snikket-server", "snikket/snikket-server");
        _ = Snikket("snikket-proxy", "snikket/snikket-web-proxy")
            .WithBindMount(snikketAcmePath, "/var/www/html/.well-known/acme-challenge");
        _ = Snikket("snikket-certs", "snikket/snikket-cert-manager")
            .WithBindMount(snikketAcmePath, "/var/www/.well-known/acme-challenge");

        _ = builder.AddContainer("snikket-portal", "snikket/snikket-web-portal", "dev")
            .WithContainerName("snikket-portal")
            .WithSnikketSettings(settings)
            .WithHomepage("Messaging", "Portal", "mdi-chat", href: $"https://chat.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("conversejs", builder.RepoPath("src/containers/xmpp-web"), "Dockerfile")
            .WithContainerName("xmpp")
            .WithEnvironment("XMPP_DOMAIN", settings.Shared.DuckDnsDomain)
            .WithEnvironment("XMPP_WS_URL", settings.Chat.Value("XMPP_WS_URL", $"wss://xmpp.{settings.Shared.DuckDnsDomain}/ws"))
            .WithEnvironment("XMPP_BOSH_URL", settings.Chat.Value("XMPP_BOSH_URL", $"https://xmpp.{settings.Shared.DuckDnsDomain}/bosh"))
            .WithLabPort(80, "conversejs")
            .WithHomepage("Messaging", "XMPP", "mdi-chat", href: $"https://xmpp.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("xmpp-ejabberd", "ghcr.io/processone/ejabberd", "latest")
            .WithContainerName("xmpp-server")
            .WithEnvironment("EJABBERD_MACRO_HOST", settings.Shared.DuckDnsDomain)
            .WithEnvironment("EJABBERD_MACRO_ADMIN", $"admin@{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("REGISTER_ADMIN_PASSWORD", settings.Secret("EJABBERD_ADMIN_PASSWORD"))
            .WithEnvironment("XMPP_DOMAIN", settings.Shared.DuckDnsDomain)
            .WithBindMount(settings.Chat.DataPath("EJABBERD_DATA", "../services-data/system/ejabberd"), "/opt/ejabberd/database")
            .WithConfigFile("configs/ejabberd/ejabberd.yml", "/opt/ejabberd/conf/ejabberd.yml", settings.Chat.DataPath("EJABBERD_CONFIG", "../services-data/system/ejabberd/conf/ejabberd.yml"))
            .WithEndpoint(5222, 5222, name: "xmpp-client")
            .WithEndpoint(5223, 5223, name: "xmpp-client-tls")
            .WithEndpoint(5269, 5269, name: "xmpp-s2s")
            .WithLabPort(5280, "xmpp-http")
            .WithLab(lab => lab.Hostname = $"xmpp.{settings.Shared.DuckDnsDomain}")
            .WithHomepage("Messaging", "XMPP", "mdi-server-network", "SERVER", $"https://xmpp.{settings.Shared.DuckDnsDomain}");

        // Matrix
        _ = builder.AddContainer("element-web", "vectorim/element-web", "latest")
            .WithContainerName("matrix")
            .WithConfigTemplate("configs/element/config.json.template", "/app", "config.json", new Dictionary<string, object> { ["DOMAIN"] = settings.Shared.DuckDnsDomain })
            .WithLabPort(80, "element-web")
            .WithHomepage("Messaging", "Matrix", "mdi-chat", href: $"https://matrix.{settings.Shared.DuckDnsDomain}");

        AddSynapse(builder, dependencies, settings);

        // Forum
        var lemmy = builder.AddContainer("lemmy", "dessalines/lemmy", "0.19.20")
            .WithContainerName("forum")
            .WithContainerNetworkAlias("lemmy")
            .WithEnvironment("LEMMY_DATABASE_URL", ReferenceExpression.Create($"postgres://lemmy:{settings.Secret("LEMMY_DB_PASSWORD")}@lemmy-db:5432/lemmy"))
            .WithEnvironment("LEMMY_HOSTNAME", $"forum.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("LEMMY_PORT", "8536")
            .WithEnvironment("LEMMY_CONFIG_LOCATION", "/config/config.hjson")
            .WithConfigTemplate("configs/lemmy/config.hjson.template", "/config", "config.hjson", new Dictionary<string, object>
            {
                ["HOSTNAME"] = $"forum.{settings.Shared.DuckDnsDomain}",
                ["DOMAIN"] = settings.Shared.DuckDnsDomain,
                ["PICTRS_API_KEY"] = settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"),
            }, escape: value => value.Replace("\\", "\\\\").Replace("\"", "\\\""))
            .WithLabPort(8536, "lemmy")
            .WaitFor(dependencies["lemmy-db"])
            .WithHomepage("Messaging", "Forum", "mdi-forum", "SERVER", $"https://forum.{settings.Shared.DuckDnsDomain}");

        var lemmyUi = builder.AddContainer("lemmy-project-ui", "dessalines/lemmy-ui", "0.19.20")
            .WithContainerName("forum-ui")
            .WithContainerNetworkAlias("lemmy-ui")
            .WithEnvironment("LEMMY_UI_LEMMY_INTERNAL_HOST", "forum:8536")
            .WithEnvironment("LEMMY_UI_LEMMY_EXTERNAL_HOST", $"forum.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("LEMMY_UI_HTTPS", "true")
            .WithBindMount(settings.Chat.DataPath("LEMMY_UI_THEMES", "../services-data/system/lemmy-project/lemmy-ui/extra_themes"), "/app/extra_themes")
            .WaitFor(lemmy)
            .WithHomepage("Messaging", "Forum", "mdi-forum", "Federated network", $"https://forum.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("lemmy-project-postfix", "mwader/postfix-relay", "latest")
            .WithContainerName("lemmy-project-postfix")
            .WithEnvironment("POSTFIX_myhostname", settings.Shared.DuckDnsDomain);

        _ = builder.AddContainer("lemmy-project-proxy", "nginx", "1-alpine")
            .WithContainerName("lemmy-project-proxy")
            .WithContainerNetworkAlias("proxy")
            .WithRepoMount("configs/lemmy/nginx_internal.conf", "/etc/nginx/nginx.conf", isReadOnly: true)
            .WithRepoMount("configs/lemmy/proxy_params", "/etc/nginx/proxy_params", isReadOnly: true)
            .WithLabPort(8536, "lemmy-project-proxy")
            .WithLab(lab => lab.PortGroup = "Messaging")
            .WithRestart("always")
            .WaitFor(dependencies["lemmy-project-pictrs"])
            .WaitFor(lemmyUi);

    }

    public static IResourceBuilder<ContainerResource> WithSnikketSettings(this IResourceBuilder<ContainerResource> resource, EnvironmentSettings settings)
    {
        return resource
            .WithEnvironment("SNIKKET_DOMAIN", settings.Chat.Value("SNIKKET_DOMAIN", $"chat.{settings.Shared.DuckDnsDomain}"))
            .WithEnvironment("SNIKKET_ADMIN_EMAIL", settings.Chat.Value("SNIKKET_ADMIN_EMAIL", $"admin@{settings.Shared.DuckDnsDomain}"));
    }

    /// <summary>
    /// Synapse without an init container: homeserver.yaml and the log config are rendered from configs/synapse/*.template
    /// into /config; the signing key is created by Synapse itself (--generate-keys).
    /// </summary>
    private static void AddSynapse(IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        const int synapseUid = 991;
        var dataPath = settings.Chat.DataPath("SYNAPSE_DATA", "../services-data/system/synapse");
        var variables = new Dictionary<string, object>
        {
            ["SERVER_NAME"] = settings.Shared.DuckDnsDomain,
            ["DB_HOST"] = "synapse-db",
            ["DB_USER"] = "synapse",
            ["DB_NAME"] = "synapse",
            ["DB_PASSWORD"] = settings.Secret("SYNAPSE_DB_PASSWORD"),
            ["MACAROON_SECRET_KEY"] = settings.Secret("SYNAPSE_MACAROON_SECRET_KEY", SecretKind.Hex32, Path.Combine(dataPath, $"{settings.Shared.DuckDnsDomain}.macaroon.secret")),
        };
        // Values sit inside YAML single quotes in the templates.
        static string Yaml(string value)
        {
            return value.Replace("'", "''");
        }

        _ = builder.AddContainer("synapse", "matrixdotorg/synapse", "v1.95.1")
            .WithContainerName("matrix-server")
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
}
