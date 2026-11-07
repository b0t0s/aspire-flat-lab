namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> MedikeepDb => Once(() => Postgres("medikeep-db", "16-alpine", "medical_records", "medapp", "MEDIKEEP_DB_PASSWORD",
        Settings.Irl.DataPath("MEDIKEEP_DB_DATA", "../services-data/system/medikeep/postgres")));

    public IResourceBuilder<ContainerResource> HealthlogDb => Once(() => Postgres("healthlog-db", "16-alpine", "healthlog", "healthlog", "HEALTHLOG_DB_PASSWORD",
        Settings.Irl.DataPath("HEALTHLOG_DB_DATA", "../services-data/system/healthlog/postgres")));

    public IResourceBuilder<ContainerResource> TradetallyDb => Once(() => Postgres("tradetally-db", "16-alpine", "tradetally", "trader", "TRADETALLY_DB_PASSWORD",
        Settings.Irl.DataPath("TRADETALLY_DB_DATA", "../services-data/system/tradetally/postgres")));

    public IResourceBuilder<ContainerResource> GrampsDb => Once(() => Postgres("gramps-db", "16-alpine", "gramps", "gramps", "GRAMPS_DB_PASSWORD",
        Settings.SmartHome.DataPath("GRAMPS_DB_DATA", "../services-data/system/gramps-db")));

    public IResourceBuilder<ContainerResource> OpenemrDb => Once(() => Add("openemr-db", "mariadb", "11.8.8")
        .WithArgs("mariadbd", "--character-set-server=utf8mb4")
        .WithEnvironment("MYSQL_ROOT_PASSWORD", Settings.Secret("OPENEMR_DB_ROOT_PASS"))
        .WithEnvironment("MYSQL_DATABASE", "openemr")
        .WithEnvironment("MYSQL_USER", "openemr")
        .WithEnvironment("MYSQL_PASSWORD", Settings.Secret("OPENEMR_DB_PASS"))
        .WithBindMount(Settings.SmartHome.DataPath("OPENEMR_DB_DATA", "../services-data/system/openemr-db"), "/var/lib/mysql")
        .WithHealthcheck("/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized", interval: "1m", retries: 3, startPeriod: "1m"));
}
