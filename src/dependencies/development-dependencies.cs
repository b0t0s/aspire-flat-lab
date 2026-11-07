namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> AtuinDb => Once(() => Postgres("atuin-db", "16-alpine", "atuin", "atuin", "ATUIN_DB_PASSWORD",
        Settings.Development.DataPath("ATUIN_DB_DATA", "../services-data/system/atuin/postgres")));

    public IResourceBuilder<ContainerResource> IdeonDb => Once(() => Postgres("ideon-db", "18-alpine", "ideon", "ideon", "IDEON_DB_PASSWORD",
        Settings.Development.DataPath("IDEON_DB_DATA", "../services-data/system/ideon/postgres")));
}
