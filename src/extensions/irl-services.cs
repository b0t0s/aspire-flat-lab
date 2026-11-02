using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
internal static class IrlServices
{
    public static void AddIrlServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("dawarich", "freikin/dawarich", "latest")
            .WithContainerName("dawarich")
            .WithEnvironment("RAILS_ENV", "production")
            .WithEnvironment("APPLICATION_HOSTS", "*")
            .WithBindMount(settings.SmartHome.DataPath("DAWARICH_DATA", "../services-data/system/dawarich"), "/usr/src/app/storage")
            .WithLabPort(3000, "dawarich")
            .WithHomepage("Knowledge", "Location", "mdi-map-marker-radius", href: $"https://locations.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithDefaultMode(LabMode.Auto)
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Household", "Phonebook", "mdi-calendar-clock", href: $"https://phonebook.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("medikeep", "ghcr.io/afairgiant/medikeep", "latest")
            .WithContainerName("medikeep")
            .WithEnvironment("DB_HOST", "medikeep-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "medical_records")
            .WithEnvironment("DB_USER", "medapp")
            .WithEnvironment("DB_PASSWORD", settings.Secret("MEDIKEEP_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("MEDIKEEP_SECRET_KEY"))
            .WithEnvironment("DEBUG", "false")
            .WithEnvironment("ENABLE_API_DOCS", "false")
            .WithEnvironment("LOG_LEVEL", "INFO")
            .WithEnvironment("ENABLE_SSL", "false")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_UPLOADS", "../services-data/system/medikeep/uploads"), "/app/uploads")
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_LOGS", "../services-data/system/medikeep/logs"), "/app/logs")
            .WithBindMount(settings.Irl.DataPath("MEDIKEEP_BACKUPS", "../services-data/system/medikeep/backups"), "/app/backups")
            .WithLabPort(8000, "medikeep")
            .WaitFor(dependencies["medikeep-db"])
            .WithHomepage("Irl", "MediKeep", "mdi-medical-bag", "Medical records", $"https://medikeep.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("parenthese-aio", "ghcr.io/iliesallali/parenthese-aio", "latest")
            .WithContainerName("parenthese")
            .WithBindMount(settings.Entertainment.DataPath("PARENTHESE_DATA", "../services-data/system/parenthese"), "/data")
            .WithLabPort(80, "parenthese-aio")
            .WithHomepage("Entertainment", "Family tree", "mdi-family-tree", "Parenthèse genealogy", $"https://familytree.{settings.Shared.DuckDnsDomain}");

    }
}
