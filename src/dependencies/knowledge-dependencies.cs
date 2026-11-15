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

    public string KiwixDataPath => Settings.Knowledge.DataPath("KIWIX_DATA", "../services-data/public/zim");

    // Offline libraries (ZIM files). The fetcher resolves the newest edition of each KIWIX_ZIMS entry ("<folder>/<name>"),
    // downloads what is missing, drops older editions, and always exits 0 so Kiwix also starts without internet.
    public IResourceBuilder<ContainerResource> KiwixFetch => Once(() => Builder.AddContainer("kiwix-fetch", "alpine", "3.21")
        .WithContainerName("kiwix-fetch")
        .WithRestart("no")
        .WithEnvironment("KIWIX_ZIMS", Settings.Knowledge.Value("KIWIX_ZIMS",
            "phet/phet_en_all phet/phet_uk_all wikipedia/wikipedia_en_astronomy_nopic wikipedia/wikipedia_en_physics_mini " +
            "wikipedia/wikipedia_en_chemistry_mini wikipedia/wikipedia_en_mathematics_mini stack_exchange/astronomy.stackexchange.com_en_all " +
            "wikipedia/wikipedia_en_top_mini wikipedia/wikipedia_uk_top_mini"))
        .WithBindMount(KiwixDataPath, "/data")
        .WithEntrypoint("/bin/sh")
        .WithArgs("-c", string.Join("\n",
            "apk add --no-cache curl >/dev/null || { echo 'no internet: keeping existing ZIM files'; exit 0; }",
            "for item in $KIWIX_ZIMS; do",
            "  folder=${item%%/*}; name=${item#*/}",
            "  latest=$(curl -fsL \"https://download.kiwix.org/zim/$folder/\" | grep -oE \"${name}_[0-9]{4}-[0-9]{2}\\.zim\" | sort -u | tail -n 1)",
            "  [ -n \"$latest\" ] || { echo \"$item: not found on download.kiwix.org\"; continue; }",
            "  if [ -f \"/data/$latest\" ]; then echo \"$latest: up to date\"; else",
            "    echo \"$latest: downloading\"",
            "    curl -fL -C - -o \"/data/$latest.part\" \"https://download.kiwix.org/zim/$folder/$latest\" && mv \"/data/$latest.part\" \"/data/$latest\" || { echo \"$latest: download failed\"; continue; }",
            "  fi",
            "  for old in /data/${name}_*.zim; do [ \"$old\" = \"/data/$latest\" ] || { [ -f \"$old\" ] && echo \"removing $old\" && rm -f \"$old\"; }; done",
            "done",
            "exit 0")));
}
