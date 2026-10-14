internal static class MediaServices
{
    public static void AddMediaServices(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
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
    }
}
