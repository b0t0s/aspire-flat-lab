namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> MedikeepDb => Once(() => Postgres("medikeep-db", "16-alpine", "medical_records", "medapp", "MEDIKEEP_DB_PASSWORD",
        Settings.Irl.DataPath("MEDIKEEP_DB_DATA", "../services-data/system/medikeep/postgres")));

    public IResourceBuilder<ContainerResource> HealthlogDb => Once(() => Postgres("healthlog-db", "16-alpine", "healthlog", "healthlog", "HEALTHLOG_DB_PASSWORD",
        Settings.Irl.DataPath("HEALTHLOG_DB_DATA", "../services-data/system/healthlog/postgres")));

    public IResourceBuilder<ContainerResource> TradetallyDb => Once(() => Postgres("tradetally-db", "16-alpine", "tradetally", "trader", "TRADETALLY_DB_PASSWORD",
        Settings.Irl.DataPath("TRADETALLY_DB_DATA", "../services-data/system/tradetally/postgres")));

    public IResourceBuilder<ContainerResource> GrampsRedis => Once(() => Add("gramps-redis", "valkey/valkey", "8-alpine"));

    public IResourceBuilder<ContainerResource> GrampsCelery => Once(() => WithGrampsWeb(Builder.AddContainer("gramps-celery", "ghcr.io/gramps-project/grampsweb", "latest")
            .WithContainerName("gramps-celery")
            .WithArgs("celery", "-A", "gramps_webapi.celery", "worker", "--loglevel=INFO", "--concurrency=2"))
        .WaitFor(GrampsRedis));

    public IResourceBuilder<ContainerResource> WithGrampsWeb(IResourceBuilder<ContainerResource> container)
    {
        var data = Settings.Irl.DataPath("GRAMPS_DATA", "../services-data/system/gramps");
        container = container
            .WithEnvironment("GRAMPSWEB_TREE", "Family")
            .WithEnvironment("GRAMPSWEB_BASE_URL", $"https://genealogy.{Settings.Shared.DuckDnsDomain}")
            .WithEnvironment("GRAMPSWEB_CELERY_CONFIG__broker_url", "redis://gramps-redis:6379/0")
            .WithEnvironment("GRAMPSWEB_CELERY_CONFIG__result_backend", "redis://gramps-redis:6379/0")
            .WithEnvironment("GRAMPSWEB_RATELIMIT_STORAGE_URI", "redis://gramps-redis:6379/1");
        foreach (var (folder, target) in new[] { ("users", "/app/users"), ("index", "/app/indexdir"), ("thumbnails", "/app/thumbnail_cache"), ("cache", "/app/cache"),
                     ("secret", "/app/secret"), ("grampsdb", "/root/.gramps/grampsdb"), ("media", "/app/media") })
        {
            container = container.WithBindMount(Path.Combine(data, folder), target);
        }
        return container;
    }

    public IResourceBuilder<ContainerResource> OpenemrDb => Once(() => Add("openemr-db", "mariadb", "11.8.9")
        .WithArgs("mariadbd", "--character-set-server=utf8mb4")
        .WithEnvironment("MYSQL_ROOT_PASSWORD", Settings.Secret("OPENEMR_DB_ROOT_PASS"))
        .WithEnvironment("MYSQL_DATABASE", "openemr")
        .WithEnvironment("MYSQL_USER", "openemr")
        .WithEnvironment("MYSQL_PASSWORD", Settings.Secret("OPENEMR_DB_PASS"))
        .WithBindMount(Settings.SmartHome.DataPath("OPENEMR_DB_DATA", "../services-data/system/openemr-db"), "/var/lib/mysql")
        .WithHealthcheck("/usr/local/bin/healthcheck.sh --su-mysql --connect --innodb_initialized", interval: "1m", retries: 3, startPeriod: "1m"));

    public string TrailkeeprBuild => Builder.RepoPath("src/containers/trailkeepr");

    public IResourceBuilder<ContainerResource> TrailkeeprDb => Once(() => Builder.AddDockerfile("trailkeepr-db", TrailkeeprBuild, "Dockerfile.db")
        .WithContainerName("postgis")
        .WithEnvironment("POSTGRES_USER", "postgres")
        .WithEnvironment("POSTGRES_PASSWORD", Settings.Secret("TRAILKEEPR_DB_PASSWORD"))
        .WithEnvironment("POSTGRES_DB", "hiking")
        .WithBindMount(Settings.Irl.DataPath("TRAILKEEPR_DB_DATA", "../services-data/system/trailkeepr/postgres"), "/var/lib/postgresql/data")
        .WithHealthcheck("pg_isready -U postgres"));

    public IResourceBuilder<ContainerResource> TrailkeeprApi => Once(() => Builder.AddDockerfile("trailkeepr-api", TrailkeeprBuild, "Dockerfile.api")
        .WithContainerName("hiking-api")
        .WithEnvironment("POSTGRES_USER", "postgres")
        .WithEnvironment("POSTGRES_PASSWORD", Settings.Secret("TRAILKEEPR_DB_PASSWORD"))
        .WithEnvironment("POSTGRES_DB", "hiking")
        .WithBindMount(Settings.Irl.DataPath("TRAILKEEPR_UPLOADS", "../services-data/system/trailkeepr/uploads"), "/app/uploads")
        .WaitFor(TrailkeeprDb));

    public IResourceBuilder<ContainerResource> AdventurelogDb => Once(() => Add("adventurelog-db", "imresamu/postgis", "16-3.5")
        .WithEnvironment("POSTGRES_DB", "adventurelog")
        .WithEnvironment("POSTGRES_USER", "adventure")
        .WithEnvironment("POSTGRES_PASSWORD", Settings.Secret("ADVENTURELOG_DB_PASSWORD"))
        .WithBindMount(Settings.Irl.DataPath("ADVENTURELOG_DB_DATA", "../services-data/system/adventurelog/postgres"), "/var/lib/postgresql/data")
        .WithHealthcheck("pg_isready -U $POSTGRES_USER -d $POSTGRES_DB"));
}
