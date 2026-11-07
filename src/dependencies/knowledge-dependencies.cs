namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> VaultisseDb => Once(() => Postgres("vaultisse-db", "16-alpine", "vaultisse", "vaultisse", "VAULTISSE_DB_PASSWORD",
        Settings.Knowledge.DataPath("VAULTISSE_DB_DATA", "../services-data/system/vaultisse/postgres")));

    public IResourceBuilder<ContainerResource> GhostDb => Once(() => Add("ghost-db", "mysql", "8.0")
        .WithEnvironment("MYSQL_ROOT_PASSWORD", Settings.Secret("GHOST_DB_ROOT_PASS"))
        .WithEnvironment("MYSQL_DATABASE", "ghost")
        .WithEnvironment("MYSQL_USER", "ghost")
        .WithEnvironment("MYSQL_PASSWORD", Settings.Secret("GHOST_DB_PASS"))
        .WithBindMount(Settings.Knowledge.DataPath("GHOST_DB_DATA", "../services-data/system/ghost-db"), "/var/lib/mysql")
        .WithHealthcheck("mysqladmin ping -h 127.0.0.1 -uroot --password=\"$MYSQL_ROOT_PASSWORD\""));

    public IResourceBuilder<ContainerResource> PaperlessDb => Once(() => Postgres("paperless-db", "16-alpine", "paperless", "paperless", "PAPERLESS_DB_PASSWORD",
        Settings.Knowledge.DataPath("PAPERLESS_DB_DATA", "../services-data/system/paperless/postgres")));

    public IResourceBuilder<ContainerResource> PaperlessRedis => Once(() => Redis("paperless-redis", Settings.Knowledge.DataPath("PAPERLESS_REDIS_DATA", "../services-data/system/paperless/redis")));
}
