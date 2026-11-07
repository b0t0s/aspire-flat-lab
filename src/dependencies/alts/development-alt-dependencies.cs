using static EnvironmentSettings;

namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> UsertourDb => Once(() => Postgres("usertour-db", "16-alpine", "usertour", "postgres", "USERTOUR_DB_PASSWORD",
        Settings.Development.DataPath("USERTOUR_DB_DATA", "../services-data/system/usertour/postgres")));

    public IResourceBuilder<ContainerResource> UsertourRedis => Once(() => Redis("usertour-redis", Settings.Development.DataPath("USERTOUR_REDIS_DATA", "../services-data/system/usertour/redis")));

    private IResourceBuilder<ParameterResource> PlausibleDbPassword => Settings.Secret("PLAUSIBLE_DB_PASSWORD");

    private IResourceBuilder<ParameterResource> ClickhousePassword => Settings.Secret("PLAUSIBLE_CLICKHOUSE_PASSWORD", aliases: "PLAUSIBLE_CLICKHO_PASSWORD");

    public IResourceBuilder<ContainerResource> PlausibleDb => Once(() => Postgres("plausible-db", "16-alpine", "plausible", "plausible", "PLAUSIBLE_DB_PASSWORD",
        Settings.SmartHome.DataPath("PLAUSIBLE_DB_DATA", "../services-data/system/plausible/postgres")));

    public IResourceBuilder<ContainerResource> PlausibleClickhouse => Once(() => Add("plausible-clickhouse", "clickhouse/clickhouse-server", "24.3-alpine")
        .WithEnvironment("CLICKHOUSE_DB", "plausible")
        .WithEnvironment("CLICKHOUSE_USER", "plausible")
        .WithEnvironment("CLICKHOUSE_PASSWORD", ClickhousePassword)
        .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
        .WithBindMount(Settings.SmartHome.DataPath("PLAUSIBLE_CLICKHOUSE_DATA", "../services-data/system/plausible/clickhouse"), "/var/lib/clickhouse"));

    public IResourceBuilder<ContainerResource> PlausibleEvents => Once(() => Add("plausible-events", "plausible/events", "latest")
        .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://plausible:{PlausibleDbPassword}@plausible-db:5432/plausible"))
        .WithEnvironment("CLICKHOUSE_DATABASE_URL", "http://plausible-clickhouse:8123/plausible")
        .WithEnvironment("CLICKHOUSE_USER", "plausible")
        .WithEnvironment("CLICKHOUSE_PASSWORD", ClickhousePassword)
        .WithEnvironment("SECRET_KEY_BASE", Settings.Secret("PLAUSIBLE_SECRET", SecretKind.LongPassword))
        .WithEnvironment("BASE_URL", $"https://analytics.{Settings.Shared.DuckDnsDomain}")
        .WaitFor(PlausibleDb)
        .WaitFor(PlausibleClickhouse));

    public IResourceBuilder<ContainerResource> KuttDb => Once(() => Postgres("kutt-db", "16-alpine", "kutt", "kutt", "KUTT_DB_PASSWORD",
        Settings.Development.DataPath("KUTT_DB_DATA", "../services-data/system/kutt/postgres")));

    public IResourceBuilder<ContainerResource> KuttRedis => Once(() => Redis("kutt-redis"));
}
