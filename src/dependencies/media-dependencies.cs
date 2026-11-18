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
            ["PROXY_PART"] = $"https://piped-proxy.{Settings.Shared.DuckDnsDomain}",
            ["API_URL"] = $"https://piped-backend.{Settings.Shared.DuckDnsDomain}",
            ["FRONTEND_URL"] = $"https://tube.{Settings.Shared.DuckDnsDomain}",
            ["PIPED_DB_PASSWORD"] = Settings.Secret("PIPED_DB_PASSWORD"),
        })
        .WithLabPort(8080, "piped-backend")
        .WithLab(lab => lab.PortGroup = "Media")
        .WaitFor(PipedDb));

    public IResourceBuilder<ContainerResource> PipedProxy => Once(() => Builder.AddContainer("piped-proxy", "1337kavin/piped-proxy", "latest")
        .WithContainerName("piped-proxy")
        .WithLabPort(8080, "piped-proxy")
        .WithLab(lab => lab.PortGroup = "Media"));

    public IResourceBuilder<ContainerResource> AudiomuseDb => Once(() => Postgres("audiomuse-db", "16-alpine", "audiomusedb", "audiomuse", "AUDIOMUSE_DB_PASSWORD",
        Settings.Media.DataPath("AUDIOMUSE_DB_DATA", "../services-data/system/audiomuse/postgres")));

    public IResourceBuilder<ContainerResource> AudiomuseWorker => Once(() => WithAudiomuse(Builder.AddContainer("audiomuse-worker", "ghcr.io/neptunehub/audiomuse-ai", "latest")
            .WithContainerName("audiomuse-worker"), "worker")
        .WaitFor(AudiomuseDb));

    public IResourceBuilder<ContainerResource> WithAudiomuse(IResourceBuilder<ContainerResource> container, string serviceType)
    {
        var data = Settings.Media.DataPath("AUDIOMUSE_DATA", "../services-data/system/audiomuse");
        return container
            .WithEnvironment("SERVICE_TYPE", serviceType)
            .WithEnvironment("TZ", Settings.Shared.TimeZone)
            .WithEnvironment("POSTGRES_HOST", "audiomuse-db")
            .WithEnvironment("POSTGRES_PORT", "5432")
            .WithEnvironment("POSTGRES_DB", "audiomusedb")
            .WithEnvironment("POSTGRES_USER", "audiomuse")
            .WithEnvironment("POSTGRES_PASSWORD", Settings.Secret("AUDIOMUSE_DB_PASSWORD"))
            .WithEnvironment("TEMP_DIR", "/app/temp_audio")
            .WithBindMount(Path.Combine(data, serviceType, "temp_audio"), "/app/temp_audio")
            .WithBindMount(Path.Combine(data, serviceType, "plugins"), "/app/plugin/installed");
    }

    // Solves Cloudflare challenges for Prowlarr only, so it starts and stops with Prowlarr.
    public IResourceBuilder<ContainerResource> Flaresolverr => Once(() => Add("flaresolverr", "ghcr.io/flaresolverr/flaresolverr", "latest")
        .WithEnvironment("LOG_LEVEL", "warning")
        .WithLabPort(8191, "flaresolverr")
        .WithLab(lab => lab.PortGroup = "Media"));
}
