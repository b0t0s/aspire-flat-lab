using apphost.cs.src;
using static EnvironmentSettings;

/// <summary>Homepage group "Entertainment" - host ports 14xxx (docs/ports.md).</summary>
internal static class EntertainmentServices
{
    public static void AddEntertainmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("minecraft", "itzg/minecraft-server", "latest")
            .WithContainerName("minecraft")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
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

        _ = dependencies["rsshub"]
            .WithEndpoint(10004, 1200, name: "rsshub")
            .WithHomepage("Infrastructure", "RSS Generator", "mdi-rss-box", href: $"https://rsshub.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("miniflux", "miniflux/miniflux", "latest")
            .WithContainerName("feed")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://miniflux:{settings.Secret("MINIFLUX_DB_PASSWORD")}@miniflux-db:5432/miniflux?sslmode=disable"))
            .WithEnvironment("RUN_MIGRATIONS", "1")
            .WithEnvironment("LISTEN_ADDR", ":8080")
            .WithEnvironment("BASE_URL", $"https://feed.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("FETCHER_ALLOW_PRIVATE_NETWORKS", "1")
            .WithEnvironment("CREATE_ADMIN", "1")
            .WithEnvironment("ADMIN_USERNAME", settings.Secret("MINIFLUX_ADMIN_USER"))
            .WithEnvironment("ADMIN_PASSWORD", settings.Secret("MINIFLUX_ADMIN_PASSWORD"))
            .WithLabPort(8080, "miniflux")
            .WaitFor(dependencies["miniflux-db"])
            .WaitFor(dependencies["rsshub"])
            .WithHomepage("Entertainment", "Feed", "si-feedly", href: $"https://feed.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("miniflux", "http://feed:8080", key: HomepageVar("MINIFLUX_WIDGET_KEY"));

        _ = builder.AddContainer("navidrome", "deluan/navidrome", "latest")
            .WithContainerName("music")
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
            .WithBindMount(settings.Entertainment.DataPath("JELLYFIN_CONFIG", "../services-data/system/jellyfin/config"), "/config")
            .WithBindMount(settings.Entertainment.DataPath("JELLYFIN_CACHE", "../services-data/system/jellyfin/cache"), "/cache")
            .WithBindMount(settings.Entertainment.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/media", isReadOnly: false)
            .WithLabPort(8096, "jellyfin")
            .WithHomepage("Entertainment", "Videos", "mdi-play-circle", href: $"https://videos.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("jellyfin", "http://videos:8096", key: HomepageVar("JELLYFIN_WIDGET_KEY"));

        _ = builder.AddContainer("kavita", "jvmilazz0/kavita", "latest")
            .WithContainerName("books")
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

        _ = builder.AddContainer("resonand", "ghcr.io/resonand-app/resonand", "0.1.0")
            .WithContainerName("resonand")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
            .WithEnvironment("RESONAND_SECRET_KEY", settings.Secret("RESONAND_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Entertainment.DataPath("RESONAND_DATA", "../services-data/system/resonand"), "/data")
            .WithLabPort(8000, "resonand")
            .WithHomepage("Entertainment", "Resonand", "mdi-voicemail", "Voice archive", $"https://resonand.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("gravity", builder.RepoPath("src/containers/Gravity"), "Dockerfile", "runner")
            .WithContainerName("gravity")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
            .WithLabPort(80, "gravity")
            .WithHomepage("Entertainment", "Gravity", "mdi-space-invaders", "Solar system simulator", $"https://gravity.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddDockerfile("solar-system", builder.RepoPath("src/containers/solar-system-map-builder"))
            .WithContainerName("solar")
            .WithStartPolicy(settings.Entertainment.StartByDefault)
            .WithConfigFile("configs/solar-system/config.yaml", "/config/config.yaml")
            .WithBindMount(settings.Entertainment.DataPath("SOLAR_SYSTEM_CACHE", "../services-data/system/solar-system"), "/cache")
            .WithLabPort(80, "solar-system")
            .WithHomepage("Entertainment", "Solar map", "mdi-earth", "Solar system maps", $"https://solarmap.{settings.Shared.DuckDnsDomain}/?epoch={DateTime.UtcNow:yyyy-MM-dd}%20{DateTime.UtcNow:HH:mm}:00");
    }
}
