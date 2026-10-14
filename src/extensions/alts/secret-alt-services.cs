using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Secrets" - host ports 16xxx; the signing apps are in "Messaging" (17xxx) (docs/ports.md).</summary>
internal static class SecretAltServices
{
    public static void AddSecretAltServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("aliasvault", "aliasvault/aliasvault", "latest")
            .WithContainerName("aliases")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("PUBLIC_URL", $"https://aliases.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Secrets.DataPath("ALIASVAULT_DATA", "../services-data/system/aliasvault"), "/app/data")
            .WithLabPort(80, "aliasvault")
            .WithHomepage("Secrets", "Aliases", "mdi-email-alert", href: $"https://aliases.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("privatebin", "privatebin/nginx-fpm-alpine", "stable")
            .WithContainerName("privatebin")
            .WithStartPolicy(settings.Secrets.StartByDefault)
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

        _ = builder.AddContainer("passbolt", "passbolt/passbolt", "latest")
            .WithContainerName("passbolt")
            .WithStartPolicy(settings.Secrets.StartByDefault)
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
            .WaitFor(dependencies["passbolt-db"])
            .WithHomepage("Secrets", "Passbolt", "mdi-key", "Team password manager", $"https://pass.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("psono-server", "psono/psono-server", "latest")
            .WithContainerName("psono-server")
            .WithStartPolicy(settings.Secrets.StartByDefault)
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
            .WaitFor(dependencies["psono-db"])
            .WithLab(lab => lab.PortGroup = "Secrets");

        _ = builder.AddContainer("docuseal", "docuseal/docuseal", "latest")
            .WithContainerName("docuseal")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("DATABASE_URL", "sqlite3:///data/docuseal.sqlite3")
            .WithEnvironment("SECRET_KEY_BASE", settings.Secret("DOCUSEAL_SECRET", SecretKind.LongPassword))
            .WithEnvironment("HOST", $"https://esign.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Secrets.DataPath("DOCUSEAL_DATA", "../services-data/system/docuseal"), "/data")
            .WithLabPort(3000, "docuseal")
            .WithHomepage("Messaging", "DocuSeal", "mdi-file-sign", "E-signatures", $"https://esign.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("documenso", "documenso/documenso", "latest")
            .WithContainerName("documenso")
            .WithStartPolicy(settings.Secrets.StartByDefault)
            .WithEnvironment("NEXTAUTH_SECRET", settings.Secret("DOCUMENSO_SECRET", SecretKind.LongPassword))
            .WithEnvironment("NEXTAUTH_URL", $"https://sign.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://documenso:{settings.Secret("DOCUMENSO_DB_PASSWORD")}@documenso-db:5432/documenso"))
            .WithEnvironment("REDIS_URL", "redis://documenso-redis:6379")
            .WithEnvironment("PORT", "3500")
            .WithEnvironment("NODE_ENV", "production")
            .WithBindMount(settings.Secrets.DataPath("DOCUMENSO_DATA", "../services-data/system/documenso"), "/data")
            .WithLabPort(3500, "documenso")
            .WaitFor(dependencies["documenso-db"])
            .WaitFor(dependencies["documenso-redis"])
            .WithHomepage("Messaging", "Documenso", "mdi-signature-freehand", "Document signing", $"https://sign.{settings.Shared.DuckDnsDomain}");
    }
}
