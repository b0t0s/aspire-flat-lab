/// <summary>Homepage group "Entertainment" - host ports 14xxx (docs/ports.md).</summary>
public static class EntertainmentServices
{
    public static void AddEntertainmentServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var app = settings.Productivity;
        var media = settings.Media;
        var shared = settings.Shared;
        var domain = shared.DuckDnsDomain;

        builder.AddContainer("miniflux", "miniflux/miniflux", "latest")
            .WithContainerName("feed")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://miniflux:{settings.Secret("MINIFLUX_DB_PASSWORD")}@miniflux-db:5432/miniflux?sslmode=disable"))
            .WithEnvironment("RUN_MIGRATIONS", "1")
            .WithEnvironment("LISTEN_ADDR", ":8080")
            .WithEnvironment("BASE_URL", $"https://feed.{domain}")
            .WithEnvironment("FETCHER_ALLOW_PRIVATE_NETWORKS", "1")
            .WithEnvironment("CREATE_ADMIN", "1")
            .WithEnvironment("ADMIN_USERNAME", app.Value("MINIFLUX_ADMIN_USER"))
            .WithEnvironment("ADMIN_PASSWORD", settings.Secret("MINIFLUX_ADMIN_PASSWORD"))
            .WithLabPort(8080, "miniflux")
            .WaitFor(dependencies["miniflux-db"])
            .WithHomepage("Entertainment", "Feed", "si-feedly", href: $"https://feed.{domain}")
            .WithHomepageWidget("miniflux", "http://feed:8080", key: HomepageVar("MINIFLUX_WIDGET_KEY"));

        builder.AddContainer("navidrome", "deluan/navidrome", "latest")
            .WithContainerName("music")
            .WithEnvironment("ND_SCANSCHEDULE", "1h")
            .WithEnvironment("ND_LOGLEVEL", "info")
            .WithEnvironment("ND_BASEURL", "/")
            .WithEnvironment("ND_MUSICFOLDER", "/music")
            .WithBindMount(media.DataPath("NAVIDROME_DATA", "../services-data/system/navidrome"), "/data")
            .WithBindMount(media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music", isReadOnly: true)
            .WithLabPort(4533, "navidrome")
            .WithHomepage("Entertainment", "Music", "mdi-music", href: $"https://music.{domain}")
            .WithHomepageWidget("navidrome", "http://music:4533",
                username: HomepageVar("NAVIDROME_WIDGET_USER"),
                token: HomepageVar("NAVIDROME_WIDGET_TOKEN"),
                salt: HomepageVar("NAVIDROME_WIDGET_SALT"));

        builder.AddContainer("jellyfin", "jellyfin/jellyfin", "latest")
            .WithContainerName("videos")
            .WithBindMount(media.DataPath("JELLYFIN_CONFIG", "../services-data/system/jellyfin/config"), "/config")
            .WithBindMount(media.DataPath("JELLYFIN_CACHE", "../services-data/system/jellyfin/cache"), "/cache")
            .WithBindMount(media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/media", isReadOnly: true)
            .WithLabPort(8096, "jellyfin")
            .WithHomepage("Entertainment", "Videos", "mdi-play-circle", href: $"https://videos.{domain}")
            .WithHomepageWidget("jellyfin", "http://videos:8096", key: HomepageVar("JELLYFIN_WIDGET_KEY"));

        builder.AddContainer("kavita", "jvmilazz0/kavita", "latest")
            .WithContainerName("books")
            .WithEnvironment("TZ", shared.TimeZone)
            .WithBindMount(media.DataPath("KAVITA_DATA", "../services-data/system/kavita"), "/kavita/config")
            .WithBindMount(media.DataPath("KAVITA_LIBRARY", "../services-data/public/books"), "/books", isReadOnly: true)
            .WithBindMount(media.DataPath("KAVITA_MANGA", "../services-data/public/manga"), "/manga", isReadOnly: true)
            .WithLabPort(5000, "kavita")
            .WithHomepage("Entertainment", "Books", "mdi-book-multiple", "Books reader", $"https://books.{domain}")
            .WithHomepageWidget("kavita", "http://books:5000",
                username: HomepageVar("KAVITA_WIDGET_USER"),
                password: HomepageVar("KAVITA_WIDGET_PASSWORD"));

        // DO AFTER START:
        // chown -R 999:999 ../services-data/system/photoview
        builder.AddContainer("photoview", "photoview/photoview", "latest")
            .WithContainerName("gallery")
            .WithEnvironment("PHOTOVIEW_DATABASE_DRIVER", "sqlite")
            .WithEnvironment("PHOTOVIEW_SQLITE_PATH", "/app/db/photoview.db")
            .WithEnvironment("PHOTOVIEW_MEDIA_CACHE", "/app/cache")
            .WithEnvironment("PHOTOVIEW_DISABLE_FACE_RECOGNITION", "true")
            .WithEnvironment("PHOTOVIEW_DISABLE_RAW_PROCESSING", "true")
            .WithBindMount(media.DataPath("PHOTOVIEW_DATA", "../services-data/system/photoview"), "/app/db")
            .WithBindMount(media.DataPath("PHOTOVIEW_PHOTOS", "../services-data/public/photos"), "/photos/")
            .WithBindMount(media.DataPath("PHOTOVIEW_CACHE", "../services-data/system/photoview/cache"), "/app/cache")
            .WithLabPort(80, "photoview")
            .WithHomepage("Entertainment", "Gallery", "mdi-image-search", href: $"https://gallery.{domain}");
    }
}
