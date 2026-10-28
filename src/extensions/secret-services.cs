using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Secrets" - host ports 16xxx; the signing apps are in "Messaging" (17xxx) (docs/ports.md).</summary>
internal static class SecretServices
{
    public static void AddSecretServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("twofauth", "2fauth/2fauth", "latest")
            .WithContainerName("twofauth")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_KEY", settings.Secret("TWOFAUTH_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("APP_URL", $"https://2fa.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithEnvironment("LOG_CHANNEL", "stderr")
            .WithBindMount(settings.Secrets.DataPath("TWOFAUTH_DATA", "../services-data/system/2fauth"), "/2fauth")
            .WithLabPort(8000, "twofauth")
            .WithHomepage("Secrets", "2FAuth", "si-authy", "2FA TOTP", $"https://2fa.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("vaultwarden", "vaultwarden/server", "latest")
            .WithContainerName("passwords")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("DOMAIN", $"https://passwords.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("SIGNUPS_ALLOWED", settings.Secrets.Value("VAULTWARDEN_SIGNUPS_ALLOWED", "true"))
            .WithEnvironment("INVITATIONS_ALLOWED", "true")
            .WithEnvironment("SHOW_PASSWORD_HINT", "false")
            .WithEnvironment("LOG_LEVEL", "info")
            .WithEnvironment("LOG_TIMESTAMP", "true")
            .WithEnvironment("ROCKET_PORT", "80")
            .WithEnvironment("ROCKET_ADDRESS", "0.0.0.0")
            .WithEnvironment("WEBSOCKET_ENABLED", "true")
            .WithEnvironment("DISABLE_ADMIN_TOKEN", "true")
            .WithEnvironment("ADMIN_TOKEN", settings.Secret("VAULTWARDEN_ADMIN_TOKEN", SecretKind.LongPassword))
            .WithEnvironment("ROCKET_WORKERS", "2")
            .WithBindMount(settings.Secrets.DataPath("VAULTWARDEN_DATA", "../services-data/system/vaultwarden"), "/data")
            .WithLabPort(80, "vaultwarden")
            .WithHomepage("Secrets", "Passwords", "mdi-shield-key", "Passwords manager", $"https://passwords.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("infisical", "infisical/infisical", "latest")
            .WithContainerName("infisical")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("SITE_URL", $"https://secrets.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_CONNECTION_URI", ReferenceExpression.Create($"postgres://infisical:{settings.Secret("INFISICAL_DB_PASSWORD")}@infisical-db:5432/infisical"))
            .WithEnvironment("REDIS_URL", "redis://infisical-redis:6379")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("INFISICAL_ENCRYPTION_KEY", SecretKind.Hex16))
            .WithEnvironment("JWT_SIGNUP_SECRET", settings.Secret("INFISICAL_JWT_SECRET", SecretKind.LongPassword))
            .WithEnvironment("JWT_REFRESH_SECRET", settings.Secret("INFISICAL_JWT_REFRESH_SECRET", SecretKind.LongPassword))
            .WithEnvironment("SMTP_HOST", "")
            .WithEnvironment("SMTP_PORT", "")
            .WithLabPort(80, "infisical")
            .WaitFor(dependencies["infisical-db"])
            .WaitFor(dependencies["infisical-redis"])
            .WithHomepage("Secrets", "Infisical", "mdi-key-variant", "Secrets manager", $"https://secrets.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("email-signature", "ghcr.io/clarkemedia/email-signature-generator", "latest")
            .WithContainerName("signature")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithLabPort(80, "email-signature")
            .WithHomepage("Development", "Signature", "mdi-email-edit", "Email signature generator", $"https://signature.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("origamivault", "ghcr.io/origamivault/origamivault", "latest")
            .WithContainerName("origami")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithLabPort(8080, "origamivault")
            .WithHomepage("Secrets", "OrigamiVault", "mdi-file-lock", "Paper backup of secrets", $"https://origami.{settings.Shared.DuckDnsDomain}");
    }
}
