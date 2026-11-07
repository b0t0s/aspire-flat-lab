using apphost.cs.src;
using static EnvironmentSettings;

internal static class MediaServices
{
    public static void AddMediaServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        var musicPath = settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music");

        _ = builder.AddContainer("flaresolverr", "ghcr.io/flaresolverr/flaresolverr", "latest")
            .WithContainerName("flaresolverr")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("LOG_LEVEL", "info")
            .WithLabPort(8191, "flaresolverr")
            .WithLab(lab => lab.PortGroup = "Media");

        IResourceBuilder<ContainerResource> LinuxServer(string name, string image, string containerName)
        {
            return builder.AddContainer(name, image, "latest")
                .WithContainerName(containerName)
                .WithEnvironment("PUID", settings.Shared.Uid)
                .WithEnvironment("PGID", settings.Shared.Gid)
                .WithEnvironment("TZ", settings.Shared.TimeZone);
        }

        _ = LinuxServer("radarr", "lscr.io/linuxserver/radarr", "indexer-films")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.Media.DataPath("RADARR_DATA", "../services-data/system/radarr"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(settings.Media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(7878, "radarr")
            .WithHomepage("Media", "Indexer Films", "mdi-movie", "Movies library", $"https://radarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("radarr", "http://indexer-films:7878", key: HomepageVar("RADARR_WIDGET_KEY"));

        _ = LinuxServer("sonarr", "lscr.io/linuxserver/sonarr", "indexer-serials")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.Media.DataPath("SONARR_DATA", "../services-data/system/sonarr"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithBindMount(settings.Media.DataPath("MEDIA_FOLDERS", "../services-data/public/movies"), "/movies")
            .WithLabPort(8989, "sonarr")
            .WithHomepage("Media", "Indexer Serials", "mdi-television", "Serials library", $"https://sonarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("sonarr", "http://indexer-serials:8989", key: HomepageVar("SONARR_WIDGET_KEY"));

        _ = LinuxServer("lidarr", "lscr.io/linuxserver/lidarr", "indexer-music")
            .WithDefaultMode(LabMode.Manual)
            .WithBindMount(settings.Media.DataPath("LIDARR_DATA", "../services-data/system/lidarr"), "/config")
            .WithBindMount(settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public/downloads"), "/downloads")
            .WithLabPort(8686, "lidarr")
            .WithHomepage("Media", "Indexer Music", "mdi-album", "Music library", $"https://lidarr.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("lidarr", "http://indexer-music:8686", key: HomepageVar("LIDARR_WIDGET_KEY"));

        const int torrentPort = 6881;
        _ = LinuxServer("qbittorrent", "lscr.io/linuxserver/qbittorrent", "torrent")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("WEBUI_PORT", "8080")
            .WithEnvironment("TORRENTING_PORT", torrentPort.ToString())
            .WithBindMount(settings.Media.DataPath("QBITTORRENT_CONFIG", "../services-data/system/qbittorrent/config"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public"), "/downloads")
            .WithLabPort(8080, "qbittorrent-web")
            .WithPublishedPort(torrentPort, torrentPort, "tcp")
            .WithPublishedPort(torrentPort, torrentPort, "udp")
            .WithHomepage("Media", "Torrent", "mdi-magnet", "", $"https://torrent.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("qbittorrent", "http://torrent:8080", username: HomepageVar("QBITTORRENT_WIDGET_USER"), password: HomepageVar("QBITTORRENT_WIDGET_PASSWORD"));

        _ = LinuxServer("prowlarr", "lscr.io/linuxserver/prowlarr", "indexer")
            .WithDefaultMode(LabMode.Auto)
            .WithBindMount(settings.Media.DataPath("PROWLARR_DATA", "../services-data/system/prowlarr"), "/config")
            .WithLabPort(9696, "prowlarr")
            .WithHomepage("Media", "Indexer", "mdi-magnify", "", $"https://indexer.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("prowlarr", "http://indexer:9696", key: HomepageVar("PROWLARR_WIDGET_KEY"));

        _ = builder.AddContainer("ryot", "ignisda/ryot", "v10")
            .WithContainerName("ryot")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("DATABASE_URL", ReferenceExpression.Create($"postgres://postgres:{settings.Secret("RYOT_DB_PASSWORD")}@ryot-db:5432/postgres"))
            .WithEnvironment("SERVER_ADMIN_ACCESS_TOKEN", settings.Secret("RYOT_ADMIN_TOKEN", SecretKind.LongPassword))
            .WithLabPort(8000, "ryot")
            .WaitFor(dependencies.RyotDb)
            .WithHomepage("Media", "Ryot", "mdi-book-multiple", "Media tracker", $"https://track.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("slskd", "slskd/slskd", "latest")
            .WithContainerName("slskd")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("SLSKD_NO_AUTH", "true")
            .WithEnvironment("SLSKD_DOWNLOADS_DIR", "/music")
            .WithEnvironment("SLSKD_SLSK_USERNAME", settings.Media.Value("SLSKD_USERNAME"))
            .WithEnvironment("SLSKD_SLSK_PASSWORD", settings.Secret("SLSKD_PASSWORD"))
            .WithBindMount(settings.Media.DataPath("SLSKD_DATA", "../services-data/system/slskd"), "/app")
            .WithBindMount(musicPath, "/music")
            .WithLab(lab =>
            {
                lab.ExtraPorts.Add((5030, 5030, "tcp"));
                lab.ExtraPorts.Add((50300, 50300, "tcp"));
            })
            .WithHomepage("Media", "Soulseek", "mdi-share-variant");

        _ = builder.AddContainer("sound-vault", "kanrim/sound-vault", "latest")
            .WithContainerName("soundvault")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("NAVIDROME_USER", settings.Media.Value("SOUNDVAULT_NAVIDROME_USER"))
            .WithEnvironment("NAVIDROME_PASS", settings.Secret("SOUNDVAULT_NAVIDROME_PASS"))
            .WithEnvironment("NAVIDROME_URL", "http://navidrome:4533")
            .WithEnvironment("SLSKD_URL", "http://slskd:5030")
            .WithEnvironment("SLSKD_API_KEY", settings.Media.Value("SOUNDVAULT_SLSKD_API_KEY"))
            .WithBindMount(musicPath, "/music")
            .WithLabPort(8080, "sound-vault")
            .WithHomepage("Media", "Sound Vault", "mdi-music-box-multiple", "", $"https://soundvault.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("music-assistant", "ghcr.io/music-assistant/server", "latest")
            .WithContainerName("music-assistant")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("LOG_LEVEL", "info")
            .WithBindMount(settings.Media.DataPath("MUSIC_ASSISTANT_DATA", "../services-data/system/music-assistant"), "/data")
            .WithBindMount(musicPath, "/media", isReadOnly: true)
            .WithHostNetwork()
            .WithHomepage("Media", "Music Assistant", "mdi-speaker", "Music library", $"https://music-assistant.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("audiomuse-ai", "neptunebhub/audiomuse-ai", "latest")
            .WithContainerName("audiomuse-ai")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("AM_LOGGING_LEVEL", "INFO")
            .WithEnvironment("AM_NAVIDROME_URL", "http://music:4533")
            .WithEnvironment("AM_NAVIDROME_USER", settings.Media.Value("NAVIDROME_WIDGET_USER", "admin"))
            .WithEnvironment("AM_NAVIDROME_PASSWORD", settings.Secret("NAVIDROME_PASSWORD", SecretKind.Manual, aliases: "NAVIDROME_WIDGET_PASSWORD"))
            .WithBindMount(settings.Media.DataPath("AUDIOMUSE_DATA", "../services-data/system/audiomuse"), "/app/data")
            .WithBindMount(settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music"), "/music", isReadOnly: true)
            .WithLabPort(8500, "audiomuse-ai")
            .WithHomepage("Media", "Sonic analysis", "mdi-waveform", "", $"https://audiomuse.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("metube", "ghcr.io/alexta69/metube", "latest")
            .WithContainerName("ytdl")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("YTDL_NIGHTLY_UPDATE_TIME", "12:00")
            .WithBindMount(settings.Media.DataPath("METUBE_DATA", "../services-data/public"), "/downloads")
            .WithLabPort(8081, "metube")
            .WithDns(settings.Shared.Dns1, settings.Shared.Dns2)
            .WithDockerExecCommand("update-yt-dlp", "Update yt-dlp", "pip install --upgrade pip && pip install --upgrade yt-dlp && yt-dlp --version")
            .WithHomepage("Media", "YT-DL", "mdi-youtube", "Youtube downloader", $"https://ytdl.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ytdlp-web-player", "matszwe02/ytdlp_web_player", "stable")
            .WithContainerName("ytdl-player")
            .WithDefaultMode(LabMode.Manual)
            .WithLabPort(5000, "ytdlp-web-player")
            .WithHomepage("Media", "YT player", "mdi-play-box", "ytdlp web player", $"https://ytdl-player.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("pinchflat-ngx", "ghcr.io/thebadfella/pinchflat-ngx", "latest")
            .WithContainerName("youtube")
            .WithDefaultMode(LabMode.Off)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Media.DataPath("PINCHFLAT_NGX_CONFIG", "../services-data/system/pinchflat-ngx/config"), "/config")
            .WithBindMount(settings.Media.DataPath("PINCHFLAT_NGX_DATA", "../services-data/public/videos/youtube"), "/downloads")
            .WithLabPort(8945, "pinchflat-ngx")
            .WithHomepage("Media", "YouTube", "mdi-youtube", "Pinchflat-NGX media manager", $"https://youtube.{settings.Shared.DuckDnsDomain}");

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
            .WithLab(lab => lab.PortGroup = "Media")
            .WaitFor(dependencies.PipedDb);

        var pipedProxy = builder.AddContainer("piped-proxy", "1337kavin/piped-proxy", "latest")
            .WithContainerName("piped-proxy")
            .WithLabPort(8080, "piped-proxy")
            .WithLab(lab => lab.PortGroup = "Media");

        _ = builder.AddContainer("piped-frontend", "1337kavin/piped-frontend", "latest")
            .WithContainerName("piped-frontend")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("BACKEND_HOSTNAME", $"pipedapi.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HTTP_MODE", "https")
            .WithLabPort(80, "piped-frontend")
            .WaitFor(pipedBackend)
            .WaitFor(pipedProxy)
            .WithHomepage("Media", "Piped", "mdi-youtube", "Privacy YouTube frontend", $"https://piped.{settings.Shared.DuckDnsDomain}");

    }
}
