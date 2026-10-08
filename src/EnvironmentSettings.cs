using Microsoft.Extensions.Configuration;

public sealed class EnvironmentSettings(IDistributedApplicationBuilder builder)
{
    private readonly LabSecrets secrets = new(builder);

    public SharedEnvironmentSettings Shared { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Development { get; } = new(builder.Configuration, builder.AppHostDirectory, "DEVELOPMENT");
    public CategoryEnvironmentSettings Tools { get; } = new(builder.Configuration, builder.AppHostDirectory, "TOOLS");
    public CategoryEnvironmentSettings Monitor { get; } = new(builder.Configuration, builder.AppHostDirectory, "MONITOR");
    public CategoryEnvironmentSettings Irl { get; } = new(builder.Configuration, builder.AppHostDirectory, "IRL");
    public CategoryEnvironmentSettings Secrets { get; } = new(builder.Configuration, builder.AppHostDirectory, "SECRETS");
    public CategoryEnvironmentSettings SmartHome { get; } = new(builder.Configuration, builder.AppHostDirectory, "SMARTHOME");
    public CategoryEnvironmentSettings Media { get; } = new(builder.Configuration, builder.AppHostDirectory, "MEDIA");
    public CategoryEnvironmentSettings Productivity { get; } = new(builder.Configuration, builder.AppHostDirectory, "PRODUCTIVITY");
    public CategoryEnvironmentSettings Knowledge { get; } = new(builder.Configuration, builder.AppHostDirectory, "KNOWLEDGE");
    public CategoryEnvironmentSettings Chat { get; } = new(builder.Configuration, builder.AppHostDirectory, "CHAT");
    public CategoryEnvironmentSettings ChatVariants { get; } = new(builder.Configuration, builder.AppHostDirectory, "CHAT_VARIANTS");

    public IResourceBuilder<ParameterResource> Secret(string key, SecretKind kind = SecretKind.Password, string? legacyFile = null, params string[] aliases) =>
        secrets.Get(key, kind, legacyFile, aliases);

    public bool StartByDefaultForResource(string resourceName)
    {
        static bool Any(string name, params string[] prefixes) => prefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));

        if (Any(resourceName, "snikket-")) return ChatVariants.StartByDefault;
        if (Any(resourceName, "ryot-", "kutt-")) return Tools.StartByDefault;
        if (Any(resourceName, "plausible-", "beszel-")) return Monitor.StartByDefault;
        if (Any(resourceName, "ghost-", "karakeep-", "linkwarden-", "paperless-", "outline-", "bookstack-", "keila-")) return Knowledge.StartByDefault;
        if (Any(resourceName, "immich-")) return Media.StartByDefault;
        if (Any(resourceName, "affine-", "miniflux-", "documenso-")) return Productivity.StartByDefault;
        if (Any(resourceName, "passbolt-", "psono-", "infisical-")) return Secrets.StartByDefault;
        if (Any(resourceName, "gramps-", "openemr-")) return Irl.StartByDefault;
        if (Any(resourceName, "synapse-", "lemmy-", "livekit-")) return Chat.StartByDefault;
        return false;
    }

    public sealed class CategoryEnvironmentSettings(IConfiguration configuration, string appHostDirectory, string category)
    {
        /// <summary>Non-secret setting from .env (usernames, feature toggles).</summary>
        public string Value(string key, string fallback = "") => configuration[key] is { Length: > 0 } value ? value : fallback;

        /// <summary>Host path; defaults live under ../services-data, a .env key only overrides it (e.g. media on another disk).</summary>
        public string DataPath(string key, string fallback) => Path.GetFullPath(Value(key, fallback), appHostDirectory);

        public bool StartByDefault => bool.TryParse(configuration[$"ASPIRE_START_{category}_BY_DEFAULT"], out var enabled) && enabled;
    }

    public sealed class SharedEnvironmentSettings
    {
        public SharedEnvironmentSettings(IConfiguration configuration, string appHostDirectory)
        {
            DataRoot = Path.GetFullPath(configuration["FLAT_LAB_DATA"] ?? "../services-data", appHostDirectory);
            DuckDnsDomain = configuration["DUCKDNS_DOMAIN"] ?? "localhost";
            TimeZone = configuration["TZ"] ?? "Europe/Kyiv";
            Uid = configuration["PUID"] ?? "1000";
            Gid = configuration["PGID"] ?? "1000";
            Dns1 = configuration["DNS1"] ?? "";
            Dns2 = configuration["DNS2"] ?? "";
            NodeIp = configuration["NODE_IP"] ?? "127.0.0.1";
        }

        public string DataRoot { get; }
        public string DuckDnsDomain { get; }
        public string TimeZone { get; }
        public string Uid { get; }
        public string Gid { get; }
        public string Dns1 { get; }
        public string Dns2 { get; }
        public string NodeIp { get; }
    }
}
