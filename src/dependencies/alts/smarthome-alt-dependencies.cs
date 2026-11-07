namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> OxicloudDb => Once(() => Postgres("oxicloud-db", "16-alpine", "oxicloud", "oxicloud", "OXICLOUD_DB_PASSWORD",
        Settings.SmartHome.DataPath("OXICLOUD_DB_DATA", "../services-data/system/oxicloud/postgres")));
}
