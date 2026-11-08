namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> OxicloudDb => Once(() => Postgres("oxicloud-db", "16-alpine", "oxicloud", "oxicloud", "OXICLOUD_DB_PASSWORD",
        Settings.SmartHome.DataPath("OXICLOUD_DB_DATA", "../services-data/system/oxicloud/postgres")));

    public IResourceBuilder<ContainerResource> SeafileDb => Once(() => Builder.AddContainer("seafile-db", "mariadb", "11.8.8")
        .WithContainerName("seafile-db")
        .WithEnvironment("MYSQL_ROOT_PASSWORD", Settings.Secret("SEAFILE_DB_ROOT_PASS"))
        .WithEnvironment("MYSQL_LOG_CONSOLE", "true")
        .WithEnvironment("DEBUG", "True")
        .WithBindMount(Settings.SmartHome.DataPath("SEAFILE_DB_DATA", "../services-data/system/seafile-db"), "/var/lib/mysql"));

    public IResourceBuilder<ContainerResource> SeafileMemcached => Once(() => Builder.AddContainer("seafile-memcached", "memcached", "1.6-alpine")
        .WithContainerName("seafile-memcached")
        .WithEntrypoint("memcached")
        .WithArgs("-m", "256"));
}
