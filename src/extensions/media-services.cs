using apphost.cs.src;
using static EnvironmentSettings;

internal static class MediaServices
{
    public static void AddMediaServices(this IDistributedApplicationBuilder builder, DependenciesService dependencies, EnvironmentSettings settings)
    {
        var musicPath = settings.Media.DataPath("NAVIDROME_MUSIC", "../services-data/public/music");

        IResourceBuilder<ContainerResource> LinuxServer(string name, string image, string containerName, bool startByDefault)
        {
            return builder.AddContainer(name, image, "latest")
                .WithContainerName(containerName)
                .WithStartPolicy(startByDefault)
                .WithEnvironment("PUID", settings.Shared.Uid)
                .WithEnvironment("PGID", settings.Shared.Gid)
                .WithEnvironment("TZ", settings.Shared.TimeZone);
        }

        _ = builder.AddContainer("metube", "ghcr.io/alexta69/metube", "latest")
            .WithContainerName("ytdl")
            .WithStartPolicy(true)
            .WithEnvironment("YTDL_NIGHTLY_UPDATE_TIME", "12:00")
            .WithBindMount(settings.Media.DataPath("METUBE_DATA", "../services-data/public"), "/downloads")
            .WithLabPort(8081, "metube")
            .WithDns(settings.Shared.Dns1, settings.Shared.Dns2)
            .WithDockerExecCommand("update-yt-dlp", "Update yt-dlp", "pip install --upgrade pip && pip install --upgrade yt-dlp && yt-dlp --version")
            .WithHomepage("Media", "YT-DL", "mdi-youtube", "Youtube downloader", $"https://ytdl.{settings.Shared.DuckDnsDomain}");

        const int torrentPort = 6881;
        _ = LinuxServer("qbittorrent", "lscr.io/linuxserver/qbittorrent", "torrent", true)
            .WithEnvironment("WEBUI_PORT", "8080")
            .WithEnvironment("TORRENTING_PORT", torrentPort.ToString())
            .WithBindMount(settings.Media.DataPath("QBITTORRENT_CONFIG", "../services-data/system/qbittorrent/config"), "/config")
            .WithBindMount(settings.Media.DataPath("DOWNLOADS_PATH", "../services-data/public"), "/downloads")
            .WithLabPort(8080, "qbittorrent-web")
            .WithPublishedPort(torrentPort, torrentPort, "tcp")
            .WithPublishedPort(torrentPort, torrentPort, "udp")
            .WithHomepage("Media", "Torrent", "mdi-magnet", "", $"https://torrent.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("qbittorrent", "http://torrent:8080", username: HomepageVar("QBITTORRENT_WIDGET_USER"), password: HomepageVar("QBITTORRENT_WIDGET_PASSWORD"));

        _ = LinuxServer("prowlarr", "lscr.io/linuxserver/prowlarr", "indexer", true)
            .WithBindMount(settings.Media.DataPath("PROWLARR_DATA", "../services-data/system/prowlarr"), "/config")
            .WithLabPort(9696, "prowlarr")
            .WithHomepage("Media", "Indexer", "mdi-magnify", "", $"https://indexer.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("prowlarr", "http://indexer:9696", key: HomepageVar("PROWLARR_WIDGET_KEY"));

        var pipedBackend = builder.AddContainer("piped-backend", "1337kavin/piped", "latest")
            .WithContainerName("piped-backend")
            .WithStartPolicy(settings.Media.StartByDefault)
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
            .WithStartPolicy(settings.Media.StartByDefault)
            .WithEnvironment("BACKEND_HOSTNAME", $"pipedapi.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("HTTP_MODE", "https")
            .WithLabPort(80, "piped-frontend")
            .WaitFor(pipedBackend)
            .WithHomepage("Media", "Piped", "mdi-youtube", "Privacy YouTube frontend", $"https://piped.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("piped-proxy", "1337kavin/piped-proxy", "latest")
            .WithContainerName("piped-proxy")
            .WithStartPolicy(settings.Media.StartByDefault)
            .WithLabPort(8080, "piped-proxy")
            .WithHomepage("Media", "Piped proxy", "mdi-google-drive", "Piped video proxy", $"https://pipedproxy.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("cliparr", "ghcr.io/techsquidtv/cliparr", "latest")
            .WithContainerName("cliparr")
            .WithStartPolicy(settings.Media.StartByDefault)
            .WithEnvironment("APP_KEY", settings.Secret("CLIPARR_APP_KEY", SecretKind.Base64))
            .WithEnvironment("CLIPARR_DATA_DIR", "/data")
            .WithBindMount(settings.Media.DataPath("CLIPARR_DATA", "../services-data/system/cliparr"), "/data")
            .WithLabPort(7171, "cliparr")
            .WithHomepage("Media", "Clips", "mdi-movie-edit", "", $"https://clips.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("slskd", "slskd/slskd", "latest")
            .WithContainerName("slskd")
            .WithStartPolicy(settings.Media.StartByDefault)
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
            .WithStartPolicy(settings.Media.StartByDefault)
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
            .WithStartPolicy(settings.Media.StartByDefault)
            .WithEnvironment("LOG_LEVEL", "info")
            .WithBindMount(settings.Media.DataPath("MUSIC_ASSISTANT_DATA", "../services-data/system/music-assistant"), "/data")
            .WithBindMount(musicPath, "/media", isReadOnly: true)
            .WithHostNetwork()
            .WithHomepage("Media", "Music Assistant", "mdi-speaker", "Music library", $"https://music-assistant.{settings.Shared.DuckDnsDomain}");
    }
}
