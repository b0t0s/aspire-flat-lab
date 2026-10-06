using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Irl" - host ports 18xxx (docs/ports.md).</summary>
public static class IrlServices
{
    public static void AddIrlServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var irl = settings.Irl;
        var domain = settings.Shared.DuckDnsDomain;

        builder.AddContainer("gramps-web", "ghcr.io/gramps-project/gramps-webapi", "latest")
            .WithContainerName("genealogy")
            .WithStartPolicy(irl.StartByDefault)
            .WithEnvironment("GRAMPS_DATABASE_URI", ReferenceExpression.Create($"postgresql://gramps:{settings.Secret("GRAMPS_DB_PASSWORD")}@gramps-db:5432/gramps"))
            .WithEnvironment("GRAMPS_SECRET_KEY", settings.Secret("GRAMPS_SECRET", SecretKind.LongPassword))
            .WithLabPort(5000, "gramps-web")
            .WaitFor(dependencies["gramps-db"])
            .WithHomepage("Irl", "Genealogy", "mdi-family-tree", href: $"https://genealogy.{domain}");

        builder.AddContainer("openemr", "openemr/openemr", "8.2.0")
            .WithContainerName("medical")
            .WithStartPolicy(irl.StartByDefault)
            .WithEnvironment("MYSQL_HOST", "openemr-db")
            .WithEnvironment("MYSQL_ROOT_PASS", settings.Secret("OPENEMR_DB_ROOT_PASS"))
            .WithEnvironment("MYSQL_USER", "openemr")
            .WithEnvironment("MYSQL_PASS", settings.Secret("OPENEMR_DB_PASS"))
            .WithEnvironment("OE_USER", irl.Value("OPENEMR_ADMIN_USER", "admin"))
            .WithEnvironment("OE_PASS", settings.Secret("OPENEMR_ADMIN_PASS"))
            .WithBindMount(irl.DataPath("OPENEMR_LOG", "../services-data/system/openemr/log"), "/var/log")
            .WithBindMount(irl.DataPath("OPENEMR_SITES", "../services-data/system/openemr/sites"), "/var/www/localhost/htdocs/openemr/sites")
            .WithLabPort(80, "openemr-http")
            .WithLabPort(443, "openemr-https", "https")
            .WaitFor(dependencies["openemr-db"])
            .WithHomepage("Irl", "Medical", "mdi-hospital", href: $"https://medical.{domain}");
    }
}
