namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> InfisicalDb => Once(() => Postgres("infisical-db", "16-alpine", "infisical", "infisical", "INFISICAL_DB_PASSWORD",
        Settings.Secrets.DataPath("INFISICAL_DB_DATA", "../services-data/system/infisical/postgres")));

    public IResourceBuilder<ContainerResource> InfisicalRedis => Once(() => Redis("infisical-redis"));

    public IResourceBuilder<ContainerResource> PassboltDb => Once(() => MariaDb("passbolt-db", "11", "passbolt", "passbolt", "PASSBOLT_DB_PASSWORD",
        Settings.Secrets.DataPath("PASSBOLT_DB_DATA", "../services-data/system/passbolt/db")));

    public IResourceBuilder<ContainerResource> PsonoDb => Once(() => MariaDb("psono-db", "11", "psono", "psono", "PSONO_DB_PASSWORD",
        Settings.Secrets.DataPath("PSONO_DB_DATA", "../services-data/system/psono/db")));

    public IResourceBuilder<ContainerResource> DocumensoDb => Once(() => Postgres("documenso-db", "16-alpine", "documenso", "documenso", "DOCUMENSO_DB_PASSWORD",
        Settings.Secrets.DataPath("DOCUMENSO_DB_DATA", "../services-data/system/documenso/postgres")));

    public IResourceBuilder<ContainerResource> DocumensoRedis => Once(() => Redis("documenso-redis"));
}
