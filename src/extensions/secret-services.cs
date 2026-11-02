using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Secrets" - host ports 16xxx; the signing apps are in "Messaging" (17xxx) (docs/ports.md).</summary>
internal static class SecretServices
{
    public static void AddSecretServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("twofauth", "2fauth/2fauth", "latest")
            .WithContainerName("twofauth")
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

        _ = builder.AddContainer("docuseal", "docuseal/docuseal", "latest")
            .WithContainerName("signature")
            .WithEnvironment("DATABASE_URL", "sqlite3:///data/docuseal.sqlite3")
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("DOCUSEAL_SECRET", SecretKind.LongPassword))
            .WithEnvironment("HOST", $"https://signature.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Secrets.DataPath("DOCUSEAL_DATA", "../services-data/system/docuseal"), "/data")
            .WithLabPort(3000, "docuseal")
            .WithHomepage("Secrets", "Signature", "mdi-file-sign", "", $"https://signature.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("email-signature", "ghcr.io/clarkemedia/email-signature-generator", "latest")
            .WithContainerName("email-signature-generator")
            .WithLabPort(80, "email-signature")
            .WithHomepage("Secrets", "Email Signature Generator", "mdi-email-edit", "Email signature generator", $"https://email-signature-generator.{settings.Shared.DuckDnsDomain}");

        // Single sign-on: Pocket ID is the passkey OIDC provider, Tinyauth puts a login in front of Caddy routes (import tinyauth in the Caddyfile).
        _ = builder.AddContainer("pocket-id", "ghcr.io/pocket-id/pocket-id", "v2")
            .WithContainerName("pocket-id")
            .WithEnvironment("APP_URL", $"https://id.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TRUST_PROXY", "true")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("POCKET_ID_ENCRYPTION_KEY", SecretKind.Base64))
            .WithBindMount(settings.Secrets.DataPath("POCKET_ID_DATA", "../services-data/system/pocket-id"), "/app/data")
            .WithLabPort(1411, "pocket-id")
            .WithHomepage("Secrets", "Pocket ID", "mdi-passport", "Passkey login (OIDC)", $"https://id.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("tinyauth", "ghcr.io/tinyauthapp/tinyauth", "v5")
            .WithContainerName("tinyauth")
            .WithEnvironment("TINYAUTH_APPURL", $"https://auth.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TINYAUTH_AUTH_USERS", settings.Secret("TINYAUTH_USERS", SecretKind.Manual))
            .WithEnvironment("TINYAUTH_AUTH_TRUSTEDPROXIES", "172.16.0.0/12")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_NAME", "Pocket ID")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_CLIENTID", settings.Secrets.Value("TINYAUTH_POCKETID_CLIENT_ID"))
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_CLIENTSECRET", settings.Secret("TINYAUTH_POCKETID_CLIENT_SECRET", SecretKind.Manual))
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_AUTHURL", $"https://id.{settings.Shared.DuckDnsDomain}/authorize")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_TOKENURL", $"https://id.{settings.Shared.DuckDnsDomain}/api/oidc/token")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_USERINFOURL", $"https://id.{settings.Shared.DuckDnsDomain}/api/oidc/userinfo")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_REDIRECTURL", $"https://auth.{settings.Shared.DuckDnsDomain}/api/oauth/callback/pocketid")
            .WithEnvironment("TINYAUTH_OAUTH_PROVIDERS_POCKETID_SCOPES", "openid email profile groups")
            .WithBindMount(settings.Secrets.DataPath("TINYAUTH_DATA", "../services-data/system/tinyauth"), "/data")
            .WithLabPort(3000, "tinyauth")
            .WithHomepage("Secrets", "Tinyauth", "mdi-lock-check", "Login in front of services", $"https://auth.{settings.Shared.DuckDnsDomain}");
    }
}
