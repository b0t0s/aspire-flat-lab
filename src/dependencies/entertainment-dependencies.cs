namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> MinifluxDb => Once(() => Postgres("miniflux-db", "16-alpine", "miniflux", "miniflux", "MINIFLUX_DB_PASSWORD",
        Settings.Entertainment.DataPath("MINIFLUX_DB_DATA", "../services-data/system/miniflux/postgres")));

    public IResourceBuilder<ContainerResource> Rsshub => Once(() => Add("rsshub", "diygod/rsshub", "latest")
        .WithEnvironment("NODE_ENV", "production"));
}
