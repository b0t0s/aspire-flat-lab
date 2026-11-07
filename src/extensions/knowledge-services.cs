using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Knowledge" - host ports 12xxx (docs/ports.md).</summary>
internal static class KnowledgeServices
{
    public static void AddKnowledgeServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("trilium", "triliumnext/trilium", "latest")
            .WithContainerName("trilium")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("USER_UID", settings.Shared.Uid)
            .WithEnvironment("USER_GID", settings.Shared.Gid)
            .WithBindMount(settings.Knowledge.DataPath("TRILIUM_DATA", "../services-data/system/trilium"), "/home/node/trilium-data")
            .WithLabPort(8080, "trilium")
            .WithHomepage("Knowledge", "Trilium", "mdi-file-tree", "", $"https://trilium.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("paperless-ngx", "ghcr.io/paperless-ngx/paperless-ngx", "latest")
            .WithContainerName("documents")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("PAPERLESS_REDIS", "redis://paperless-redis:6379")
            .WithEnvironment("PAPERLESS_DBENGINE", "postgresql")
            .WithEnvironment("PAPERLESS_DBHOST", "paperless-db")
            .WithEnvironment("PAPERLESS_DBNAME", "paperless")
            .WithEnvironment("PAPERLESS_DBUSER", "paperless")
            .WithEnvironment("PAPERLESS_DBPASS", settings.Secret("PAPERLESS_DB_PASSWORD"))
            .WithEnvironment("PAPERLESS_SECRET_KEY", settings.Secret("PAPERLESS_SECRET", SecretKind.LongPassword))
            .WithEnvironment("PAPERLESS_TIME_ZONE", settings.Shared.TimeZone)
            .WithEnvironment("PAPERLESS_URL", $"https://documents.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("USERMAP_UID", settings.Shared.Uid)
            .WithEnvironment("USERMAP_GID", settings.Shared.Gid)
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_DATA", "../services-data/system/paperless/data"), "/usr/src/paperless/data")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_MEDIA", "../services-data/system/paperless/media"), "/usr/src/paperless/media")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_EXPORT", "../services-data/system/paperless/export"), "/usr/src/paperless/export")
            .WithBindMount(settings.SmartHome.DataPath("PAPERLESS_CONSUME", "../services-data/system/paperless/consume"), "/usr/src/paperless/consume")
            .WithLabPort(8000, "paperless-ngx")
            .WaitFor(dependencies.PaperlessRedis)
            .WaitFor(dependencies.PaperlessDb)
            .WithHomepage("Knowledge", "Documents", "mdi-file-pdf-box", href: $"https://documents.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("paperlessngx", "http://documents:8000", key: HomepageVar("PAPERLESS_WIDGET_KEY"));

        _ = builder.AddContainer("faved", "denho/faved", "latest")
            .WithContainerName("bookmarks")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("DATA_DIR", "/data")
            .WithEnvironment("SECRET_KEY", settings.Secret("FAVED_SECRET"))
            .WithBindMount(settings.Knowledge.DataPath("FAVED_DATA", "../services-data/system/faved"), "/var/www/html/storage/")
            .WithLabPort(80, "faved")
            .WithHomepage("Knowledge", "Bookmarks", "si-pocket", href: $"https://bookmarks.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("memos", "neosmemo/memos", "stable")
            .WithContainerName("favourites")
            .WithDefaultMode(LabMode.Auto)
            .WithBindMount(settings.Knowledge.DataPath("MEMOS_DATA", "../services-data/system/memos"), "/var/opt/memos")
            .WithLabPort(5230, "memos")
            .WithHomepage("Knowledge", "Favourites", "mdi-note-multiple", "Microblog", $"https://favourites.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ghost", "ghost", "5")
            .WithContainerName("blog")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("database__client", "mysql")
            .WithEnvironment("database__connection__host", "ghost-db")
            .WithEnvironment("database__connection__user", "ghost")
            .WithEnvironment("database__connection__password", settings.Secret("GHOST_DB_PASS"))
            .WithEnvironment("database__connection__database", "ghost")
            .WithEnvironment("url", $"https://blog.{settings.Shared.DuckDnsDomain}")
            .WithBindMount(settings.Knowledge.DataPath("GHOST_DATA", "../services-data/system/ghost"), "/var/lib/ghost/content")
            .WithLabPort(2368, "ghost")
            .WaitFor(dependencies.GhostDb)
            .WithHomepage("Knowledge", "Blog", "si-ghost", "Blog and posts", $"https://blog.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("vaultisse", "ghcr.io/albertamat/vaultisse", "latest")
            .WithContainerName("vaultisse")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DB_HOST", "vaultisse-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "vaultisse")
            .WithEnvironment("DB_USER", "vaultisse")
            .WithEnvironment("DB_PASSWORD", settings.Secret("VAULTISSE_DB_PASSWORD"))
            .WithBindMount(settings.Knowledge.DataPath("VAULTISSE_LOGS", "../services-data/system/vaultisse/logs"), "/app/logs")
            .WithLabPort(3000, "vaultisse")
            .WaitFor(dependencies.VaultisseDb)
            .WithHomepage("Knowledge", "Book lending", "mdi-bookshelf", "", $"https://vaultisse.{settings.Shared.DuckDnsDomain}");

        // Offline libraries (ZIM files). The fetcher resolves the newest edition of each KIWIX_ZIMS entry ("<folder>/<name>"),
        // downloads what is missing, drops older editions, and always exits 0 so Kiwix also starts without internet.
        var zimPath = settings.Knowledge.DataPath("KIWIX_DATA", "../services-data/public/zim");
        var kiwixFetch = builder.AddContainer("kiwix-fetch", "alpine", "3.21")
            .WithContainerName("kiwix-fetch")
            .WithRestart("no")
            .WithEnvironment("KIWIX_ZIMS", settings.Knowledge.Value("KIWIX_ZIMS",
                "phet/phet_en_all phet/phet_uk_all wikipedia/wikipedia_en_astronomy_nopic wikipedia/wikipedia_en_physics_mini " +
                "wikipedia/wikipedia_en_chemistry_mini wikipedia/wikipedia_en_mathematics_mini stack_exchange/astronomy.stackexchange.com_en_all " +
                "wikipedia/wikipedia_en_top_mini wikipedia/wikipedia_uk_top_mini"))
            .WithBindMount(zimPath, "/data")
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
                "exit 0"));

        _ = builder.AddContainer("kiwix", "ghcr.io/kiwix/kiwix-serve", "latest")
            .WithContainerName("kiwix")
            .WithDefaultMode(LabMode.Manual)
            .WithArgs("*.zim")
            .WithBindMount(zimPath, "/data")
            .WithLabPort(8080, "kiwix")
            .WaitForCompletion(kiwixFetch)
            .WithDockerExecCommand("list-zims", "List ZIM files", "ls -lh /data/*.zim", "Library")
            .WithHomepage("Knowledge", "Kiwix", "mdi-library", "Offline Wikipedia, PhET simulations, Stack Exchange", $"https://kiwix.{settings.Shared.DuckDnsDomain}");
    }
}
