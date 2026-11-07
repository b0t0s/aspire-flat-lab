namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> RyotDb => Once(() => Postgres("ryot-db", "18-alpine", "postgres", "postgres", "RYOT_DB_PASSWORD",
        Settings.Media.DataPath("RYOT_DB_DATA", "../services-data/system/ryot/postgres"), "/var/lib/postgresql"));

    public IResourceBuilder<ContainerResource> PipedDb => Once(() => Postgres("piped-db", "16-alpine", "piped", "piped", "PIPED_DB_PASSWORD",
        Settings.Media.DataPath("PIPED_DB_DATA", "../services-data/system/piped/postgres")));
}
