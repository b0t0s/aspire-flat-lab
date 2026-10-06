using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Secrets" - host ports 16xxx; the signing apps are in "Messaging" (17xxx) (docs/ports.md).</summary>
public static class SecretServices
{
    public static void AddSecretServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var secrets = settings.Secrets;
        var app = settings.Productivity;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("twofauth", "2fauth/2fauth", "latest")
            .WithContainerName("twofauth")
            .WithEnvironment("APP_ENV", "production")
            .WithEnvironment("APP_KEY", settings.Secret("TWOFAUTH_APP_KEY", SecretKind.LaravelKey))
            .WithEnvironment("APP_URL", $"https://2fa.{domain}")
            .WithEnvironment("DB_CONNECTION", "sqlite")
            .WithEnvironment("LOG_CHANNEL", "stderr")
            .WithBindMount(secrets.DataPath("TWOFAUTH_DATA", "../services-data/system/2fauth"), "/2fauth")
            .WithLabPort(8000, "twofauth")
            .WithHomepage("Secrets", "2FAuth", "si-authy", "2FA TOTP", $"https://2fa.{domain}");

        builder.AddContainer("vaultwarden", "vaultwarden/server", "latest")
            .WithContainerName("passwords")
            .WithEnvironment("DOMAIN", $"https://passwords.{domain}")
            .WithEnvironment("SIGNUPS_ALLOWED", secrets.Value("VAULTWARDEN_SIGNUPS_ALLOWED", "true"))
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
            .WithBindMount(secrets.DataPath("VAULTWARDEN_DATA", "../services-data/system/vaultwarden"), "/data")
            .WithLabPort(80, "vaultwarden")
            .WithHomepage("Secrets", "Passwords", "mdi-shield-key", "Passwords manager", $"https://passwords.{domain}");

        builder.AddContainer("aliasvault", "aliasvault/aliasvault", "latest")
            .WithContainerName("aliases")
            .WithStartPolicy(secrets.StartByDefault)
            .WithEnvironment("PUBLIC_URL", $"https://aliases.{domain}")
            .WithBindMount(secrets.DataPath("ALIASVAULT_DATA", "../services-data/system/aliasvault"), "/app/data")
            .WithLabPort(80, "aliasvault")
            .WithHomepage("Secrets", "Aliases", "mdi-email-alert", href: $"https://aliases.{domain}");

        builder.AddContainer("documenso", "documenso/documenso", "latest")
            .WithContainerName("documenso")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("DOCUMENSO_SECRET", SecretKind.LongPassword))
            .WithEnvironment("NEXTAUTH_URL", $"https://sign.{domain}")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://documenso:{settings.Secret("DOCUMENSO_DB_PASSWORD")}@documenso-db:5432/documenso"))
            .WithEnvironment("REDIS_URL", "redis://documenso-redis:6379")
            .WithEnvironment("PORT", "3500")
            .WithEnvironment("NODE_ENV", "production")
            .WithBindMount(app.DataPath("DOCUMENSO_DATA", "../services-data/system/documenso"), "/data")
            .WithLabPort(3500, "documenso")
            .WaitFor(dependencies["documenso-db"])
            .WaitFor(dependencies["documenso-redis"])
            .WithHomepage("Messaging", "Documenso", "mdi-signature-freehand", "Document signing", $"https://sign.{domain}");

        builder.AddContainer("docuseal", "docuseal/docuseal", "latest")
            .WithContainerName("docuseal")
            .WithStartPolicy(app.StartByDefault)
            .WithEnvironment("DATABASE_URL", "sqlite3:///data/docuseal.sqlite3")
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("DOCUSEAL_SECRET", SecretKind.LongPassword))
            .WithEnvironment("HOST", $"https://esign.{domain}")
            .WithBindMount(app.DataPath("DOCUSEAL_DATA", "../services-data/system/docuseal"), "/data")
            .WithLabPort(3000, "docuseal")
            .WithHomepage("Messaging", "DocuSeal", "mdi-file-sign", "E-signatures", $"https://esign.{domain}");

        builder.AddContainer("privatebin", "privatebin/nginx-fpm-alpine", "stable")
            .WithContainerName("privatebin")
            .WithStartPolicy(secrets.StartByDefault)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("PHP_TZ", shared.TimeZone)
            .WithBindMount(secrets.DataPath("PRIVATEBIN_DATA", "../services-data/system/privatebin"), "/srv/data")
            .WithLabPort(8080, "privatebin")
            .WithLab(lab =>
            {
                lab.ReadOnly = true;
                lab.Tmpfs.Add("/tmp:nodev,noexec,mode=1777");
                lab.Tmpfs.Add("/run:nodev,exec,mode=1777");
            })
            .WithHomepage("Secrets", "PrivateBin", "mdi-content-paste", "Zero-knowledge pastebin", $"https://paste.{domain}");

        builder.AddContainer("passbolt", "passbolt/passbolt", "latest")
            .WithContainerName("passbolt")
            .WithStartPolicy(secrets.StartByDefault)
            .WithEnvironment("APP_FULL_BASE_URL", $"https://pass.{domain}")
            .WithEnvironment("DATASOURCES_DEFAULT_HOST", "passbolt-db")
            .WithEnvironment("DATASOURCES_DEFAULT_PORT", "3306")
            .WithEnvironment("DATASOURCES_DEFAULT_DATABASE", "passbolt")
            .WithEnvironment("DATASOURCES_DEFAULT_USERNAME", "passbolt")
            .WithEnvironment("DATASOURCES_DEFAULT_PASSWORD", settings.Secret("PASSBOLT_DB_PASSWORD"))
            .WithEnvironment("EMAIL_DEFAULT_FROM", $"admin@{domain}")
            .WithEnvironment("EMAIL_TRANSPORT_DEFAULT_HOST", "")
            .WithEnvironment("EMAIL_TRANSPORT_DEFAULT_PORT", "")
            .WithLabPort(80, "passbolt")
            .WaitFor(dependencies["passbolt-db"])
            .WithHomepage("Secrets", "Passbolt", "mdi-key", "Team password manager", $"https://pass.{domain}");

        builder.AddContainer("psono-server", "psono/psono-server", "latest")
            .WithContainerName("psono-server")
            .WithStartPolicy(secrets.StartByDefault)
            .WithEnvironment("DEBUG", "False")
            .WithEnvironment("ALLOWED_HOSTS", $"psono.{domain}")
            .WithEnvironment("DB_TYPE", "mysql")
            .WithEnvironment("DB_HOST", "psono-db")
            .WithEnvironment("DB_NAME", "psono")
            .WithEnvironment("DB_USER", "psono")
            .WithEnvironment("DB_PASSWORD", settings.Secret("PSONO_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("PSONO_SECRET", SecretKind.LongPassword))
            .WithEnvironment("EMAIL_URL", "")
            .WithBindMount(secrets.DataPath("PSONO_DATA", "../services-data/system/psono"), "/data")
            .WithLabPort(80, "psono")
            .WaitFor(dependencies["psono-db"])
            .WithLab(lab => lab.PortGroup = "Secrets");

        builder.AddContainer("infisical", "infisical/infisical", "latest")
            .WithContainerName("infisical")
            .WithStartPolicy(secrets.StartByDefault)
            .WithEnvironment("SITE_URL", $"https://secrets.{domain}")
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
            .WithHomepage("Secrets", "Infisical", "mdi-key-variant", "Secrets manager", $"https://secrets.{domain}");
    }
}
