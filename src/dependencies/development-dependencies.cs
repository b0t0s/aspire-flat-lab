namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> AtuinDb => Once(() => Postgres("atuin-db", "16-alpine", "atuin", "atuin", "ATUIN_DB_PASSWORD",
        Settings.Development.DataPath("ATUIN_DB_DATA", "../services-data/system/atuin/postgres")));

    public IResourceBuilder<ContainerResource> IdeonDb => Once(() => Postgres("ideon-db", "18-alpine", "ideon", "ideon", "IDEON_DB_PASSWORD",
        Settings.Development.DataPath("IDEON_DB_DATA", "../services-data/system/ideon/postgres")));

    public string TermixDataPath => Settings.SmartHome.DataPath("TERMIX_DATA", "../services-data/system/termix");

    public IResourceBuilder<ContainerResource> Guacd => Once(() => Builder.AddContainer("guacd", "guacamole/guacd", "1.6.0")
        .WithContainerName("guacd")
        .WithDefaultMode(LabMode.Auto)
        .WithBindMount(TermixDataPath, "/termix-data"));
}
