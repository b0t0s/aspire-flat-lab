using static EnvironmentSettings;

namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> UsertourDb => Once(() => Postgres("usertour-db", "16-alpine", "usertour", "postgres", "USERTOUR_DB_PASSWORD",
        Settings.Development.DataPath("USERTOUR_DB_DATA", "../services-data/system/usertour/postgres")));

    public IResourceBuilder<ContainerResource> UsertourRedis => Once(() => Redis("usertour-redis", Settings.Development.DataPath("USERTOUR_REDIS_DATA", "../services-data/system/usertour/redis")));

    private IResourceBuilder<ParameterResource> ClickhousePassword => Settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD");

    public IResourceBuilder<ContainerResource> PlausibleDb => Once(() => Postgres("plausible-db", "16-alpine", "plausible", "plausible", "PLAUSIBLE_DB_PASSWORD",
        Settings.SmartHome.DataPath("PLAUSIBLE_DB_DATA", "../services-data/system/plausible/postgres")));

    public IResourceBuilder<ContainerResource> PlausibleClickhouse => Once(() => Add("plausible-clickhouse", "clickhouse/clickhouse-server", "24.12-alpine")
        .WithEnvironment("CLICKHOUSE_DB", "plausible")
        .WithEnvironment("CLICKHOUSE_USER", "plausible")
        .WithEnvironment("CLICKHOUSE_PASSWORD", ClickhousePassword)
        .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
        .WithBindMount(Settings.SmartHome.DataPath("PLAUSIBLE_CLICKHOUSE_DATA", "../services-data/system/plausible/clickhouse"), "/var/lib/clickhouse"));

    public IResourceBuilder<ContainerResource> KuttDb => Once(() => Postgres("kutt-db", "16-alpine", "kutt", "kutt", "KUTT_DB_PASSWORD",
        Settings.Development.DataPath("KUTT_DB_DATA", "../services-data/system/kutt/postgres")));

    public IResourceBuilder<ContainerResource> KuttRedis => Once(() => Redis("kutt-redis"));

    public IResourceBuilder<ContainerResource> CalagopusRedis => Once(() => Builder.AddContainer("calagopus-redis", "ghcr.io/calagopus/valkey", "latest")
        .WithContainerName("calagopus-redis")
        .WithArgs("--protected-mode", "no", "--save", "900", "1")
        .WithBindMount(Settings.Development.DataPath("CALAGOPUS_REDIS_DATA", "../services-data/system/calagopus-redis"), "/data"));

    public IResourceBuilder<ContainerResource> CalagopusDb => Once(() => Builder.AddContainer("calagopus-db", "ghcr.io/calagopus/pgautoupgrade", "18-alpine")
        .WithContainerName("calagopus-db")
        .WithEnvironment("TZ", Settings.Shared.TimeZone)
        .WithEnvironment("POSTGRES_USER", "panel")
        .WithEnvironment("POSTGRES_PASSWORD", Settings.Secret("CALAGOPUS_DB_PASSWORD"))
        .WithEnvironment("POSTGRES_DB", "panel")
        .WithEnvironment("PGDATA", "/data")
        .WithBindMount(Settings.Development.DataPath("CALAGOPUS_DB_DATA", "../services-data/system/calagopus-db"), "/data")
        .WithHealthcheck("pg_isready -U panel"));
}
