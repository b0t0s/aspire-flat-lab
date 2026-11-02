using Microsoft.Extensions.Configuration;

internal sealed class EnvironmentSettings(IDistributedApplicationBuilder builder)
{
    private readonly LabSecrets secrets = new(builder);

    public SharedEnvironmentSettings Shared { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Chat { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Media { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Secrets { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Knowledge { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings SmartHome { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Development { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Entertainment { get; } = new(builder.Configuration, builder.AppHostDirectory);
    public CategoryEnvironmentSettings Irl { get; } = new(builder.Configuration, builder.AppHostDirectory);

    public IResourceBuilder<ParameterResource> Secret(string key, SecretKind kind = SecretKind.Password,
        string? legacyFile = null, params string[] aliases)
    {
        return secrets.Get(key, kind, legacyFile, aliases);
    }

    internal enum SecretKind
    {
        /// <summary>32 alphanumeric characters.</summary>
        Password,
        /// <summary>64 alphanumeric characters (Phoenix / Rails secret_key_base).</summary>
        LongPassword,
        /// <summary>32 random bytes as 64 lowercase hex characters (openssl rand -hex 32).</summary>
        Hex32,
        /// <summary>16 random bytes as 32 lowercase hex characters (openssl rand -hex 16).</summary>
        Hex16,
        /// <summary>48 random bytes as base64 (openssl rand -base64 48).</summary>
        Base64,
        /// <summary>Laravel APP_KEY: "base64:" + 32 random bytes.</summary>
        LaravelKey,
        /// <summary>Never generated: must be provided in .env (tokens issued by another system).</summary>
        Manual,
    }

    internal sealed class CategoryEnvironmentSettings(IConfiguration configuration, string appHostDirectory)
    {
        public string Value(string key, string fallback = "")
        {
            return configuration[key] is { Length: > 0 } value ? value : fallback;
        }

        public string DataPath(string key, string fallback)
        {
            return Path.GetFullPath(Value(key, fallback), appHostDirectory);
        }
    }

    internal sealed class SharedEnvironmentSettings(IConfiguration configuration, string appHostDirectory)
    {
        public string DataRoot { get; } = Path.GetFullPath(configuration["FLAT_LAB_DATA"] ?? "../services-data", appHostDirectory);
        public string DuckDnsDomain { get; } = configuration["DUCKDNS_DOMAIN"] ?? "localhost";
        public string TimeZone { get; } = configuration["TZ"] ?? "Europe/Kyiv";
        public string Uid { get; } = configuration["PUID"] ?? "1000";
        public string Gid { get; } = configuration["PGID"] ?? "1000";
        public string Dns1 { get; } = configuration["DNS1"] ?? "";
        public string Dns2 { get; } = configuration["DNS2"] ?? "";
        public string NodeIp { get; } = configuration["NODE_IP"] ?? "127.0.0.1";
    }
}
