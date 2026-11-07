namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> NextcloudDb => Once(() => Postgres("nextcloud-db", "16-alpine", "nextcloud", "nextcloud", "NEXTCLOUD_DB_PASSWORD",
        Settings.SmartHome.DataPath("NEXTCLOUD_DB_DATA", "../services-data/system/nextcloud/postgres")));

    public IResourceBuilder<ContainerResource> NextcloudRedis => Once(() => Redis("nextcloud-redis"));
}
