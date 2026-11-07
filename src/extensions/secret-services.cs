using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Secrets" - host ports 16xxx; the signing apps are in "Messaging" (17xxx) (docs/ports.md).</summary>
internal static class SecretServices
{
    public static void AddSecretServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("origamivault", "ghcr.io/origamivault/origamivault", "latest")
            .WithContainerName("origami")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(8080, "origamivault")
            .WithHomepage("Secrets", "OrigamiVault", "mdi-file-lock", "Paper backup of secrets", $"https://origami.{settings.Shared.DuckDnsDomain}");

        // Single sign-on: Pocket ID is the passkey OIDC provider, Tinyauth puts a login in front of Caddy routes (import tinyauth in the Caddyfile).
        _ = builder.AddContainer("tinyauth", "ghcr.io/tinyauthapp/tinyauth", "v5")
            .WithContainerName("tinyauth")
            .WithDefaultMode(LabMode.Auto)
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

        _ = builder.AddContainer("pocket-id", "ghcr.io/pocket-id/pocket-id", "v2")
            .WithContainerName("pocket-id")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("APP_URL", $"https://id.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TRUST_PROXY", "true")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("POCKET_ID_ENCRYPTION_KEY", SecretKind.Base64))
            .WithBindMount(settings.Secrets.DataPath("POCKET_ID_DATA", "../services-data/system/pocket-id"), "/app/data")
            .WithLabPort(1411, "pocket-id")
            .WithHomepage("Secrets", "Pocket ID", "mdi-passport", "Passkey login (OIDC)", $"https://id.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("twofauth", "2fauth/2fauth", "latest")
            .WithContainerName("twofauth")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_KEY", settings.Secret("TWOFAUTH_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("APP_URL", $"https://2fa.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithEnvironment("LOG_CHANNEL", "stderr")
            .WithBindMount(settings.Secrets.DataPath("TWOFAUTH_DATA", "../services-data/system/2fauth"), "/2fauth")
            .WithLabPort(8000, "twofauth")
            .WithHomepage("Secrets", "2FAuth", "si-authy", "2FA TOTP", $"https://2fa.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("aliasvault", "aliasvault/aliasvault", "latest")
            .WithContainerName("aliases")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("PUBLIC_URL", $"https://aliases.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Secrets.DataPath("ALIASVAULT_DATA", "../services-data/system/aliasvault"), "/app/data")
            .WithLabPort(80, "aliasvault")
            .WithHomepage("Secrets", "Aliases", "mdi-email-alert", href: $"https://aliases.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("vaultwarden", "vaultwarden/server", "latest")
            .WithContainerName("passwords")
            .WithDefaultMode(LabMode.Manual)
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

        _ = builder.AddContainer("privatebin", "privatebin/nginx-fpm-alpine", "stable")
            .WithContainerName("privatebin")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("PHP_TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Secrets.DataPath("PRIVATEBIN_DATA", "../services-data/system/privatebin"), "/srv/data")
            .WithLabPort(8080, "privatebin")
            .WithLab(lab =>
            {
                lab.ReadOnly = true;
                lab.Tmpfs.Add("/tmp:nodev,noexec,mode=1777");
                lab.Tmpfs.Add("/run:nodev,exec,mode=1777");
            })
            .WithHomepage("Secrets", "PrivateBin", "mdi-content-paste", "Zero-knowledge pastebin", $"https://paste.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("infisical", "infisical/infisical", "latest")
            .WithContainerName("infisical")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("SITE_URL", $"https://secrets.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_CONNECTION_URI", ReferenceExpression.Create($"postgres://infisical:{settings.Secret("INFISICAL_DB_PASSWORD")}@infisical-db:5432/infisical"))
            .WithEnvironment("REDIS_URL", "redis://infisical-redis:6379")
            .WithEnvironment("ENCRYPTION_KEY", settings.Secret("INFISICAL_ENCRYPTION_KEY", SecretKind.Hex16))
            .WithEnvironment("JWT_SIGNUP_SECRET", settings.Secret("INFISICAL_JWT_SECRET", SecretKind.LongPassword))
            .WithEnvironment("JWT_REFRESH_SECRET", settings.Secret("INFISICAL_JWT_REFRESH_SECRET", SecretKind.LongPassword))
            .WithEnvironment("SMTP_HOST", "")
            .WithEnvironment("SMTP_PORT", "")
            .WithLabPort(80, "infisical")
            .WaitFor(dependencies.InfisicalDb)
            .WaitFor(dependencies.InfisicalRedis)
            .WithHomepage("Secrets", "Infisical", "mdi-key-variant", "Secrets manager", $"https://secrets.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("passbolt", "passbolt/passbolt", "latest")
            .WithContainerName("passbolt")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("APP_FULL_BASE_URL", $"https://pass.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DATASOURCES_DEFAULT_HOST", "passbolt-db")
            .WithEnvironment("DATASOURCES_DEFAULT_PORT", "3306")
            .WithEnvironment("DATASOURCES_DEFAULT_DATABASE", "passbolt")
            .WithEnvironment("DATASOURCES_DEFAULT_USERNAME", "passbolt")
            .WithEnvironment("DATASOURCES_DEFAULT_PASSWORD", settings.Secret("PASSBOLT_DB_PASSWORD"))
            .WithEnvironment("EMAIL_DEFAULT_FROM", $"admin@{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("EMAIL_TRANSPORT_DEFAULT_HOST", "")
            .WithEnvironment("EMAIL_TRANSPORT_DEFAULT_PORT", "")
            .WithLabPort(80, "passbolt")
            .WaitFor(dependencies.PassboltDb)
            .WithHomepage("Secrets", "Passbolt", "mdi-key", "Team password manager", $"https://pass.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("psono-server", "psono/psono-server", "latest")
            .WithContainerName("psono-server")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DEBUG", "False")
            .WithEnvironment("ALLOWED_HOSTS", $"psono.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DB_TYPE", "mysql")
            .WithEnvironment("DB_HOST", "psono-db")
            .WithEnvironment("DB_NAME", "psono")
            .WithEnvironment("DB_USER", "psono")
            .WithEnvironment("DB_PASSWORD", settings.Secret("PSONO_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("PSONO_SECRET", SecretKind.LongPassword))
            .WithEnvironment("EMAIL_URL", "")
            .WithBindMount(settings.Secrets.DataPath("PSONO_DATA", "../services-data/system/psono"), "/data")
            .WithLabPort(80, "psono")
            .WaitFor(dependencies.PsonoDb)
            .WithLab(lab => lab.PortGroup = "Secrets");

        _ = builder.AddContainer("documenso", "documenso/documenso", "latest")
            .WithContainerName("documenso")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("DOCUMENSO_SECRET", SecretKind.LongPassword))
            .WithEnvironment("NEXTAUTH_URL", $"https://sign.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://documenso:{settings.Secret("DOCUMENSO_DB_PASSWORD")}@documenso-db:5432/documenso"))
            .WithEnvironment("REDIS_URL", "redis://documenso-redis:6379")
            .WithEnvironment("PORT", "3500")
            .WithEnvironment("NODE_ENV", "production")
            .WithBindMount(settings.Secrets.DataPath("DOCUMENSO_DATA", "../services-data/system/documenso"), "/data")
            .WithLabPort(3500, "documenso")
            .WaitFor(dependencies.DocumensoDb)
            .WaitFor(dependencies.DocumensoRedis)
            .WithHomepage("Secrets", "Document signing", "mdi-signature-freehand", "", $"https://sign.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("docuseal", "docuseal/docuseal", "latest")
            .WithContainerName("signature")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DATABASE_URL", "sqlite3:///data/docuseal.sqlite3")
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("DOCUSEAL_SECRET", SecretKind.LongPassword))
            .WithEnvironment("HOST", $"https://signature.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Secrets.DataPath("DOCUSEAL_DATA", "../services-data/system/docuseal"), "/data")
            .WithLabPort(3000, "docuseal")
            .WithHomepage("Secrets", "Signature", "mdi-file-sign", "", $"https://signature.{settings.Shared.DuckDnsDomain}");

    }
}
