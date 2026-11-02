using apphost.cs.src;
using static EnvironmentSettings;

internal static class MediaAltServices
{
    public static void AddMediaAltServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        IResourceBuilder<ContainerResource> LinuxServer(string name, string image, string containerName)
        {
            return builder.AddContainer(name, image, "latest")
                .WithContainerName(containerName)
                .WithEnvironment("PUID", settings.Shared.Uid)
                .WithEnvironment("PGID", settings.Shared.Gid)
                .WithEnvironment("TZ", settings.Shared.TimeZone);
        }

        _ = LinuxServer("radarr", "lscr.io/linuxserver/radarr", "indexer-films")
            .WithBindMount(settings.Media.DataPath("RADARR_DATA", "../services-data/system/radarr"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(settings.Media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(7878, "radarr")
            .WithHomepage("Media", "Indexer Films", "mdi-movie", "Movies library", $"https://radarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("radarr", "http://indexer-films:7878", key: HomepageVar("RADARR_WIDGET_KEY"));

        _ = LinuxServer("sonarr", "lscr.io/linuxserver/sonarr", "indexer-serials")
            .WithBindMount(settings.Media.DataPath("SONARR_DATA", "../services-data/system/sonarr"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(settings.Media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(8989, "sonarr")
            .WithHomepage("Media", "Indexer Serials", "mdi-television", "Serials library", $"https://sonarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("sonarr", "http://indexer-serials:8989", key: HomepageVar("SONARR_WIDGET_KEY"));

        _ = LinuxServer("lidarr", "lscr.io/linuxserver/lidarr", "indexer-music")
            .WithBindMount(settings.Media.DataPath("LIDARR_DATA", "../services-data/system/lidarr"), "/config")
            .WithBindMount(settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithLabPort(8686, "lidarr")
            .WithHomepage("Media", "Indexer Music", "mdi-album", "Music library", $"https://lidarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("lidarr", "http://indexer-music:8686", key: HomepageVar("LIDARR_WIDGET_KEY"));

        _ = builder.AddContainer("ytdlp-web-player", "matszwe02/ytdlp_web_player", "stable")
            .WithContainerName("ytdl-player")
            .WithLabPort(5000, "ytdlp-web-player")
            .WithHomepage("Media", "YT player", "mdi-play-box", "ytdlp web player", $"https://ytdl-player.{settings.Shared.DuckDnsDomain}");

        var pipedBackend = builder.AddContainer("piped-backend", "1337kavin/piped", "latest")
            .WithContainerName("piped-backend")
            .WithConfigTemplate("configs/piped/config.properties.template", "/app", "config.properties", new Dictionary<string, object>
            {
                ["PROXY_PART"] = $"https://pipedproxy.{settings.Shared.DuckDnsDomain}",
                ["API_URL"] = $"https://pipedapi.{settings.Shared.DuckDnsDomain}",
                ["FRONTEND_URL"] = $"https://piped.{settings.Shared.DuckDnsDomain}",
                ["PIPED_DB_PASSWORD"] = settings.Secret("PIPED_DB_PASSWORD"),
            })
            .WithLabPort(8080, "piped-backend")
            .WaitFor(dependencies["piped-db"])
            .WithHomepage("Media", "Piped API", "mdi-youtube", "Piped backend", $"https://pipedapi.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("piped-frontend", "1337kavin/piped-frontend", "latest")
            .WithContainerName("piped-frontend")
            .WithEnvironment("BACKEND_HOSTNAME", $"pipedapi.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HTTP_MODE", "https")
            .WithLabPort(80, "piped-frontend")
            .WaitFor(pipedBackend)
            .WithHomepage("Media", "Piped", "mdi-youtube", "Privacy YouTube frontend", $"https://piped.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("piped-proxy", "1337kavin/piped-proxy", "latest")
            .WithContainerName("piped-proxy")
            .WithLabPort(8080, "piped-proxy")
            .WithHomepage("Media", "Piped proxy", "mdi-google-drive", "Piped video proxy", $"https://pipedproxy.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("resonand", "ghcr.io/resonand-app/resonand", "0.1.0")
            .WithContainerName("resonand")
            .WithEnvironment("RESONAND_SECRET_KEY", settings.Secret("RESONAND_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Entertainment.DataPath("RESONAND_DATA", "../services-data/system/resonand"), "/data")
            .WithLabPort(8000, "resonand")
            .WithHomepage("Entertainment", "Resonand", "mdi-voicemail", "Voice archive", $"https://resonand.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ryot", "ignisda/ryot", "v10")
            .WithContainerName("ryot")
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://postgres:{settings.Secret("RYOT_DB_PASSWORD")}@ryot-db:5432/postgres"))
            .WithEnvironment("SERVER_ADMIN_ACCESS_TOKEN", settings.Secret("RYOT_ADMIN_TOKEN", SecretKind.LongPassword))
            .WithLabPort(8000, "ryot")
            .WaitFor(dependencies["ryot-db"])
            .WithHomepage("Media", "Ryot", "mdi-book-multiple", "Media tracker", $"https://track.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("audiomuse-ai", "neptunebhub/audiomuse-ai", "latest")
            .WithContainerName("audiomuse-ai")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("AM_LOGGING_LEVEL", "INFO")
            .WithEnvironment("AM_NAVIDROME_URL", "http://music:4533")
            .WithEnvironment("AM_NAVIDROME_USER", settings.Media.Value("NAVIDROME_WIDGET_USER", "admin"))
            .WithEnvironment("AM_NAVIDROME_PASSWORD", settings.Secret("NAVIDROME_PASSWORD", SecretKind.Manual, aliases: "NAVIDROME_WIDGET_PASSWORD"))
            .WithBindMount(settings.Media.DataPath("AUDIOMUSE_DATA", "../services-data/system/audiomuse"), "/app/data")
            .WithBindMount(settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music", isReadOnly: true)
            .WithLabPort(8500, "audiomuse-ai")
            .WithHomepage("Media", "AudioMuse-AI", "mdi-waveform", "Sonic analysis + AI playlists", $"https://audiomuse.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("immich-server", "ghcr.io/immich-app/immich-server", "release")
            .WithContainerName("immich-server")
            .WithEnvironment("DB_HOSTNAME", "immich-postgres")
            .WithEnvironment("DB_USERNAME", "immich")
            .WithEnvironment("DB_PASSWORD", settings.Secret("IMMICH_DB_PASSWORD"))
            .WithEnvironment("DB_DATABASE_NAME", "immich")
            .WithEnvironment("REDIS_HOSTNAME", "immich-redis")
            .WithEnvironment("TYPESENSE_API_KEY", settings.Secret("IMMICH_TYPESENSE_KEY"))
            .WithEnvironment("TYPESENSE_HOST", "immich-typesense")
            .WithEnvironment("IMMICH_NO_MACHINE_LEARNING", "true")
            .WithBindMount(settings.Media.DataPath("IMMICH_EXTERNAL_LOCATION", "../services-data/public/photos"), "/mnt/external-storage")
            .WithBindMount(settings.Media.DataPath("IMMICH_UPLOAD", "../services-data/public/photos"), "/usr/src/app/upload")
            .WithBindMount(settings.Media.DataPath("IMMICH_DATA", "../services-data/system/immich/config"), "/usr/src/app/config")
            .WithLabPort(2283, "immich-server")
            .WaitFor(dependencies["immich-redis"])
            .WaitFor(dependencies["immich-postgres"])
            .WaitFor(dependencies["immich-typesense"])
            .WithHomepage("Media", "Immich", "mdi-image-multiple", "Photo gallery", $"https://photos.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("immich", "http://immich-server:2283", key: HomepageVar("IMMICH_WIDGET_KEY"), version: "2");
    }
}
