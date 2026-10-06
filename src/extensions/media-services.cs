using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

/// <summary>Homepage group "Media" - host ports 13xxx; BitTorrent keeps 6881 (docs/ports.md).</summary>
public static class MediaServices
{
    public static void AddMediaServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var shared = settings.Shared;
        var media = settings.Media;
        var tools = settings.Tools;
        var domain = shared.DuckDnsDomain;

        IResourceBuilder<ContainerResource> LinuxServer(string name, string image, string containerName, bool startByDefault) =>
            builder.AddContainer(name, image, "latest")
                .WithContainerName(containerName)
                .WithStartPolicy(startByDefault)
                .WithEnvironment("PUID", shared.Uid)
                .WithEnvironment("PGID", shared.Gid)
                .WithEnvironment("TZ", shared.TimeZone);

        builder.AddContainer("immich-server", "ghcr.io/immich-app/immich-server", "release")
            .WithContainerName("immich-server")
            .WithStartPolicy(media.StartByDefault)
            .WithEnvironment("DB_HOSTNAME", "immich-postgres")
            .WithEnvironment("DB_USERNAME", "immich")
            .WithEnvironment("DB_PASSWORD", settings.Secret("IMMICH_DB_PASSWORD"))
            .WithEnvironment("DB_DATABASE_NAME", "immich")
            .WithEnvironment("REDIS_HOSTNAME", "immich-redis")
            .WithEnvironment("TYPESENSE_API_KEY", settings.Secret("IMMICH_TYPESENSE_KEY"))
            .WithEnvironment("TYPESENSE_HOST", "immich-typesense")
            .WithEnvironment("IMMICH_NO_MACHINE_LEARNING", "true")
            .WithBindMount(media.DataPath("IMMICH_EXTERNAL_LOCATION", "../services-data/public/photos"), "/mnt/external-storage")
            .WithBindMount(media.DataPath("IMMICH_UPLOAD", "../services-data/public/photos"), "/usr/src/app/upload")
            .WithBindMount(media.DataPath("IMMICH_DATA", "../services-data/system/immich/config"), "/usr/src/app/config")
            .WithLabPort(2283, "immich-server")
            .WaitFor(dependencies["immich-redis"])
            .WaitFor(dependencies["immich-postgres"])
            .WaitFor(dependencies["immich-typesense"])
            .WithHomepage("Media", "Immich", "mdi-image-multiple", "Photo gallery", $"https://photos.{domain}")
            .WithHomepageWidget("immich", "http://immich-server:2283", key: HomepageVar("IMMICH_WIDGET_KEY"), version: "2");

        builder.AddContainer("ryot", "ignisda/ryot", "v10")
            .WithContainerName("ryot")
            .WithStartPolicy(tools.StartByDefault)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://postgres:{settings.Secret("RYOT_DB_PASSWORD")}@ryot-db:5432/postgres"))
            .WithEnvironment("SERVER_ADMIN_ACCESS_TOKEN", settings.Secret("RYOT_ADMIN_TOKEN", SecretKind.LongPassword))
            .WithLabPort(8000, "ryot")
            .WaitFor(dependencies["ryot-db"])
            .WithHomepage("Media", "Ryot", "mdi-book-multiple", "Media tracker", $"https://track.{domain}");

        builder.AddContainer("metube", "ghcr.io/alexta69/metube", "latest")
            .WithContainerName("ytdl")
            .WithEnvironment("YTDL_NIGHTLY_UPDATE_TIME", "12:00")
            .WithBindMount(media.DataPath("METUBE_DATA", "../services-data/public"), "/downloads")
            .WithLabPort(8081, "metube")
            .WithDns(shared.Dns1, shared.Dns2)
            .WithDockerExecCommand("update-yt-dlp", "Update yt-dlp", "pip install --upgrade pip && pip install --upgrade yt-dlp && yt-dlp --version")
            .WithHomepage("Media", "YT-DL", "mdi-youtube", "Youtube downloader", $"https://ytdl.{domain}");

        const int torrentPort = 6881;
        LinuxServer("qbittorrent", "lscr.io/linuxserver/qbittorrent", "torrent", true)
            .WithStartPolicy(false)
            .WithEnvironment("WEBUI_PORT", "8080")
            .WithEnvironment("TORRENTING_PORT", torrentPort.ToString())
            .WithBindMount(media.DataPath("QBITTORRENT_CONFIG", "../services-data/system/qbittorrent/config"), "/config")
            .WithBindMount(media.DataPath("DOWNLOADS_PATH", "../services-data/public"), "/downloads")
            .WithLabPort(8080, "qbittorrent-web")
            .WithPublishedPort(torrentPort, torrentPort, "tcp")
            .WithPublishedPort(torrentPort, torrentPort, "udp")
            .WithHomepage("Media", "Torrent", "mdi-magnet", "Torrent client", $"https://torrent.{domain}")
            .WithHomepageWidget("qbittorrent", "http://torrent:8080", username: HomepageVar("QBITTORRENT_WIDGET_USER"), password: HomepageVar("QBITTORRENT_WIDGET_PASSWORD"));

        LinuxServer("prowlarr", "lscr.io/linuxserver/prowlarr", "indexer", true)
            .WithBindMount(media.DataPath("PROWLARR_DATA", "../services-data/system/prowlarr"), "/config")
            .WithLabPort(9696, "prowlarr")
            .WithHomepage("Media", "Indexer", "mdi-magnify", "Torrents indexer & manager", $"https://indexer.{domain}")
            .WithHomepageWidget("prowlarr", "http://indexer:9696", key: HomepageVar("PROWLARR_WIDGET_KEY"));

        LinuxServer("radarr", "lscr.io/linuxserver/radarr", "indexer-films", media.StartByDefault)
            .WithBindMount(media.DataPath("RADARR_DATA", "../services-data/system/radarr"), "/config")
            .WithBindMount(media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(7878, "radarr")
            .WithHomepage("Media", "Indexer Films", "mdi-movie", "Movies library", $"https://radarr.{domain}")
            .WithHomepageWidget("radarr", "http://indexer-films:7878", key: HomepageVar("RADARR_WIDGET_KEY"));

        LinuxServer("sonarr", "lscr.io/linuxserver/sonarr", "indexer-serials", media.StartByDefault)
            .WithBindMount(media.DataPath("SONARR_DATA", "../services-data/system/sonarr"), "/config")
            .WithBindMount(media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(8989, "sonarr")
            .WithHomepage("Media", "Indexer Serials", "mdi-television", "Serials library", $"https://sonarr.{domain}")
            .WithHomepageWidget("sonarr", "http://indexer-serials:8989", key: HomepageVar("SONARR_WIDGET_KEY"));

        LinuxServer("lidarr", "lscr.io/linuxserver/lidarr", "indexer-music", media.StartByDefault)
            .WithBindMount(media.DataPath("LIDARR_DATA", "../services-data/system/lidarr"), "/config")
            .WithBindMount(media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music")
            .WithBindMount(media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithLabPort(8686, "lidarr")
            .WithHomepage("Media", "Indexer Music", "mdi-album", "Music library", $"https://lidarr.{domain}")
            .WithHomepageWidget("lidarr", "http://indexer-music:8686", key: HomepageVar("LIDARR_WIDGET_KEY"));

        builder.AddContainer("audiomuse-ai", "neptunebhub/audiomuse-ai", "latest")
            .WithContainerName("audiomuse-ai")
            .WithStartPolicy(media.StartByDefault)
            .WithEnvironment("TZ", shared.TimeZone)
            .WithEnvironment("AM_LOGGING_LEVEL", "INFO")
            .WithEnvironment("AM_NAVIDROME_URL", "http://music:4533")
            .WithEnvironment("AM_NAVIDROME_USER", media.Value("NAVIDROME_WIDGET_USER", "admin"))
            .WithEnvironment("AM_NAVIDROME_PASSWORD", settings.Secret("NAVIDROME_PASSWORD", SecretKind.Manual, aliases: "NAVIDROME_WIDGET_PASSWORD"))
            .WithBindMount(media.DataPath("AUDIOMUSE_DATA", "../services-data/system/audiomuse"), "/app/data")
            .WithBindMount(media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music", isReadOnly: true)
            .WithLabPort(8500, "audiomuse-ai")
            .WithHomepage("Media", "AudioMuse-AI", "mdi-waveform", "Sonic analysis + AI playlists", $"https://audiomuse.{domain}");
    }
}
