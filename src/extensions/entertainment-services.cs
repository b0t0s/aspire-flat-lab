using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Entertainment" - host ports 14xxx (docs/ports.md).</summary>
internal static class EntertainmentServices
{
    public static void AddEntertainmentServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("minecraft", "itzg/minecraft-server", "latest")
            .WithContainerName("minecraft")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("EULA", "TRUE")
            .WithEnvironment("TYPE", "PAPER")
            .WithEnvironment("DIFFICULTY", "hard")
            .WithEnvironment("LEVEL_TYPE", "AMPLIFIED")
            .WithEnvironment("ICON", "/data/server-icon.png")
            .WithEnvironment("OVERRIDE_ICON", "true")
            .WithEnvironment("ENABLE_RCON", "false")
            .WithEnvironment("ONLINE_MODE", "false")
            .WithEnvironment("MEMORY", "2G")
            .WithEnvironment("MOTD", "Flat-lab")
            .WithBindMount(settings.Entertainment.DataPath("MINECRAFT_CONFIG", "configs/minecraft"), "/config", isReadOnly: true)
            .WithBindMount(settings.Entertainment.DataPath("MINECRAFT_DATA", "../services-data/system/minecraft"), "/data")
            .WithEndpoint(25565, 25565, name: "minecraft")
            .WithHomepage("Entertainment", "minecraft", "mdi-minecraft", "Game server")
            .WithHomepageWidget("minecraft", "udp://minecraft:25565");

        _ = builder.AddContainer("dm-hero", "ghcr.io/flo0806/dm-hero", "latest")
            .WithContainerName("dm-hero")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("NODE_ENV", "production")
            .WithEnvironment("NITRO_PORT", "3000")
            .WithEnvironment("NITRO_HOST", "0.0.0.0")
            .WithBindMount(settings.Entertainment.DataPath("DM_HERO_DATA", "../services-data/system/dm-hero/data"), "/app/data")
            .WithBindMount(settings.Entertainment.DataPath("DM_HERO_UPLOADS", "../services-data/system/dm-hero/uploads"), "/app/.output/public/uploads")
            .WithLabPort(3000, "dm-hero")
            .WithHomepage("Entertainment", "DM Hero", "mdi-dice-d20", "D&D campaign manager", $"https://dm-hero.{settings.Shared.DuckDnsDomain}");

        _ = dependencies.Rsshub
            .WithEndpoint(10004, 1200, name: "rsshub")
            .WithHomepage("Infrastructure", "RSS Generator", "mdi-rss-box", href: $"https://rsshub.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("miniflux", "miniflux/miniflux", "latest")
            .WithContainerName("feed")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://miniflux:{settings.Secret("MINIFLUX_DB_PASSWORD")}@miniflux-db:5432/miniflux?sslmode=disable"))
            .WithEnvironment("RUN_MIGRATIONS", "1")
            .WithEnvironment("LISTEN_ADDR", ":8080")
            .WithEnvironment("BASE_URL", $"https://feed.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("FETCHER_ALLOW_PRIVATE_NETWORKS", "1")
            .WithEnvironment("CREATE_ADMIN", "1")
            .WithEnvironment("ADMIN_USERNAME", settings.Secret("MINIFLUX_ADMIN_USER"))
            .WithEnvironment("ADMIN_PASSWORD", settings.Secret("MINIFLUX_ADMIN_PASSWORD"))
            .WithLabPort(8080, "miniflux")
            .WaitFor(dependencies.MinifluxDb)
            .WaitFor(dependencies.Rsshub)
            .WithHomepage("Entertainment", "Feed", "si-feedly", href: $"https://feed.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("miniflux", "http://feed:8080", key: HomepageVar("MINIFLUX_WIDGET_KEY"));

        _ = builder.AddContainer("navidrome", "deluan/navidrome", "latest")
            .WithContainerName("music")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("ND_SCANSCHEDULE", "1h")
            .WithEnvironment("ND_LOGLEVEL", "info")
            .WithEnvironment("ND_BASEURL", "/")
            .WithEnvironment("ND_MUSICFOLDER", "/music")
            .WithBindMount(settings.Entertainment.DataPath("NAVIDROME_DATA", "../services-data/system/navidrome"), "/data")
            .WithBindMount(settings.Entertainment.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music", isReadOnly: false)
            .WithLabPort(4533, "navidrome")
            .WithHomepage("Entertainment", "Music", "mdi-music", href: $"https://music.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("navidrome", "http://music:4533",
                username: HomepageVar("NAVIDROME_WIDGET_USER"),
                token: HomepageVar("NAVIDROME_WIDGET_TOKEN"),
                salt: HomepageVar("NAVIDROME_WIDGET_SALT"));

        _ = builder.AddContainer("jellyfin", "jellyfin/jellyfin", "latest")
            .WithContainerName("videos")
            .WithDefaultMode(LabMode.Auto)
            .WithBindMount(settings.Entertainment.DataPath("JELLYFIN_CONFIG", "../services-data/system/jellyfin/config"), "/config")
            .WithBindMount(settings.Entertainment.DataPath("JELLYFIN_CACHE", "../services-data/system/jellyfin/cache"), "/cache")
            .WithBindMount(settings.Entertainment.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/media", isReadOnly: false)
            .WithLabPort(8096, "jellyfin")
            .WithHomepage("Entertainment", "Videos", "mdi-play-circle", href: $"https://videos.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("jellyfin", "http://videos:8096", key: HomepageVar("JELLYFIN_WIDGET_KEY"));

        _ = builder.AddContainer("kavita", "jvmilazz0/kavita", "latest")
            .WithContainerName("books")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Entertainment.DataPath("KAVITA_DATA", "../services-data/system/kavita"), "/kavita/config")
            .WithBindMount(settings.Entertainment.DataPath("KAVITA_LIBRARY", "../services-data/public/books"), "/books", isReadOnly: true)
            .WithLabPort(5000, "kavita")
            .WithHomepage("Entertainment", "Books", "mdi-book-multiple", "Books reader", $"https://books.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("kavita", "http://books:5000",
                username: HomepageVar("KAVITA_WIDGET_USER"),
                password: HomepageVar("KAVITA_WIDGET_PASSWORD"));

        // DO AFTER START:
        // chown -R 999:999 ../services-data/system/photoview
        _ = builder.AddContainer("photoview", "photoview/photoview", "latest")
            .WithContainerName("gallery")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("PHOTOVIEW_DATABASE_DRIVER", "sqlite")
            .WithEnvironment("PHOTOVIEW_SQLITE_PATH", "/app/db/photoview.db")
            .WithEnvironment("PHOTOVIEW_MEDIA_CACHE", "/app/cache")
            .WithEnvironment("PHOTOVIEW_DISABLE_FACE_RECOGNITION", "true")
            .WithEnvironment("PHOTOVIEW_DISABLE_RAW_PROCESSING", "true")
            .WithBindMount(settings.Entertainment.DataPath("PHOTOVIEW_DATA", "../services-data/system/photoview"), "/app/db")
            .WithBindMount(settings.Entertainment.DataPath("PHOTOVIEW_PHOTOS", "../services-data/public/photos"), "/photos/")
            .WithBindMount(settings.Entertainment.DataPath("PHOTOVIEW_CACHE", "../services-data/system/photoview/cache"), "/app/cache")
            .WithLabPort(80, "photoview")
            .WithHomepage("Entertainment", "Gallery", "mdi-image-search", href: $"https://gallery.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("solar-system", builder.RepoPath("src/containers/solar-system-map-builder"))
            .WithContainerName("solar")
            .WithDefaultMode(LabMode.Auto)
            .WithRepoMount("configs/solar-system", "/config", isReadOnly: true)
            .WithBindMount(settings.Entertainment.DataPath("SOLAR_SYSTEM_CACHE", "../services-data/system/solar-system"), "/cache")
            .WithLabPort(80, "solar-system")
            .WithHomepage("Entertainment", "Solar map", "mdi-earth", "Live solar system, time travel and moments", $"https://solarmap.{settings.Shared.DuckDnsDomain}/");

        _ = builder.AddDockerfile("gravity", builder.RepoPath("src/containers/Gravity"), "Dockerfile", "runner")
            .WithContainerName("gravity")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(80, "gravity")
            .WithHomepage("Entertainment", "Gravity", "mdi-space-invaders", "Solar system simulator", $"https://gravity.{settings.Shared.DuckDnsDomain}");

        // Desktop Stellarium streamed to the browser (linuxserver Selkies base): full star catalogs, works offline.
        _ = builder.AddDockerfile("stellarium", builder.RepoPath("src/containers/stellarium"))
            .WithContainerName("stellarium")
            .WithEnvironment("PUID", settings.Shared.Uid)
            .WithEnvironment("PGID", settings.Shared.Gid)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("PIXELFLUX_WAYLAND", "false")
            .WithBindMount(settings.Entertainment.DataPath("STELLARIUM_DATA", "../services-data/system/stellarium"), "/config")
            .WithLab(lab => lab.ShmSize = "1gb")
            .WithLabPort(3000, "stellarium")
            .WithHomepage("Entertainment", "Sky", "mdi-star-shooting", "Stellarium planetarium", $"https://sky.{settings.Shared.DuckDnsDomain}");
    }
}
