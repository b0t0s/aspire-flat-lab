namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> AffineDb => Once(() => Postgres("affine-db", "16-alpine", "affine", "affine", "AFFINE_DB_PASSWORD",
        Settings.Knowledge.DataPath("AFFINE_DB_DATA", "../services-data/system/affine-db")));

    public IResourceBuilder<ContainerResource> KarakeepMeilisearch => Once(() => Meilisearch("karakeep-meilisearch", "v1.55.0", "KARAKEEP_MEILI_KEY",
        Settings.Knowledge.DataPath("KARAKEEP_MEILI_DATA", "../services-data/system/karakeep/meilisearch")));

    public IResourceBuilder<ContainerResource> KarakeepChrome => Once(() => Add("karakeep-chrome", "ghcr.io/karakeep-app/karakeep-chrome", "release")
        .WithArgs("--disable-gpu", "--disable-dev-shm-usage", "--hide-scrollbars", "--disable-blink-features=AutomationControlled", "--window-size=1440,900"));

    public IResourceBuilder<ContainerResource> LinkwardenDb => Once(() => Postgres("linkwarden-db", "16-alpine", "linkwarden", "linkwarden", "LINKWARDEN_DB_PASSWORD",
        Settings.Knowledge.DataPath("LINKWARDEN_DB_DATA", "../services-data/system/linkwarden/postgres")));

    public IResourceBuilder<ContainerResource> LinkwardenMeilisearch => Once(() => Meilisearch("linkwarden-meilisearch", "v1.13.3", "LINKWARDEN_MEILI_KEY",
        Settings.Knowledge.DataPath("LINKWARDEN_MEILI_DATA", "../services-data/system/linkwarden/meilisearch")));

    public IResourceBuilder<ContainerResource> OutlineDb => Once(() => Postgres("outline-db", "16-alpine", "outline", "outline", "OUTLINE_DB_PASSWORD",
        Settings.Knowledge.DataPath("OUTLINE_DB_DATA", "../services-data/system/outline-db")));

    public IResourceBuilder<ContainerResource> OutlineRedis => Once(() => Redis("outline-redis"));

    public IResourceBuilder<ContainerResource> BookstackDb => Once(() => MariaDb("bookstack-db", "11", "bookstack", "bookstack", "BOOKSTACK_DB_PASSWORD",
        Settings.Knowledge.DataPath("BOOKSTACK_DB_DATA", "../services-data/system/bookstack-db")));

    public IResourceBuilder<ContainerResource> JoplinDb => Once(() => Postgres("joplin-db", "16-alpine", "joplin", "joplin", "JOPLIN_DB_PASSWORD",
        Settings.Knowledge.DataPath("JOPLIN_DB_DATA", "../services-data/system/joplin/postgres")));
}
