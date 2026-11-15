namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> RyotDb => Once(() => Postgres("ryot-db", "18-alpine", "postgres", "postgres", "RYOT_DB_PASSWORD",
        Settings.Media.DataPath("RYOT_DB_DATA", "../services-data/system/ryot/postgres"), "/var/lib/postgresql"));

    public IResourceBuilder<ContainerResource> PipedDb => Once(() => Postgres("piped-db", "16-alpine", "piped", "piped", "PIPED_DB_PASSWORD",
        Settings.Media.DataPath("PIPED_DB_DATA", "../services-data/system/piped/postgres")));

    public IResourceBuilder<ContainerResource> PipedBackend => Once(() => Builder.AddContainer("piped-backend", "1337kavin/piped", "latest")
        .WithContainerName("piped-backend")
        .WithConfigTemplate("configs/piped/config.properties.template", "/app", "config.properties", new Dictionary<string, object>
        {
            ["PROXY_PART"] = $"https://pipedproxy.{Settings.Shared.DuckDnsDomain}",
            ["API_URL"] = $"https://pipedapi.{Settings.Shared.DuckDnsDomain}",
            ["FRONTEND_URL"] = $"https://piped.{Settings.Shared.DuckDnsDomain}",
            ["PIPED_DB_PASSWORD"] = Settings.Secret("PIPED_DB_PASSWORD"),
        })
        .WithLabPort(8080, "piped-backend")
        .WithLab(lab => lab.PortGroup = "Media")
        .WaitFor(PipedDb));

    public IResourceBuilder<ContainerResource> PipedProxy => Once(() => Builder.AddContainer("piped-proxy", "1337kavin/piped-proxy", "latest")
        .WithContainerName("piped-proxy")
        .WithLabPort(8080, "piped-proxy")
        .WithLab(lab => lab.PortGroup = "Media"));
}
