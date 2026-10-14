using static EnvironmentSettings;

internal static class SmartHomeServices
{
    public static void AddSmartHomeServices(this IDistributedApplicationBuilder builder, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("netalertx", "jokobsk/netalertx", "latest")
            .WithContainerName("network")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("GR_TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.SmartHome.DataPath("NETALERTX_DATA_PATH", "../services-data/system/netalertx"), "/data")
            .WithHostNetwork()
            .WithLab(lab =>
            {
                lab.Privileged = true;
                lab.Tmpfs.Add("/tmp:rw,nosuid,nodev,size=256m");
            })
            .WithHomepage("Infrastructure", "Network", "mdi-network", href: $"https://network.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("netalertx", "http://host.docker.internal:20211", key: HomepageVar("NETALERTX_WIDGET_KEY"), version: HomepageVar("NETALERTX_WIDGET_VERSION"));

        var mySpeedHostPort = int.Parse(settings.SmartHome.Value("MYSPEED_PORT", "5216"), System.Globalization.CultureInfo.InvariantCulture);
        _ = builder.AddContainer("myspeed", "germannewsmaker/myspeed", "latest")
            .WithContainerName("speed")
            .WithStartPolicy(settings.SmartHome.StartByDefault)
            .WithBindMount(settings.SmartHome.DataPath("MYSPEED_DATA_PATH", "../services-data/system/myspeed"), "/config")
            .WithPublishedPort(mySpeedHostPort, 5216)
            .WithHomepage("Infrastructure", "Speed", "mdi-speedometer", href: $"https://speed.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("myspeed", "http://speed:5216");

        _ = builder.AddContainer("radicale", "ghcr.io/kozea/radicale", "stable")
            .WithContainerName("phonebook")
            .WithStartPolicy(true)
            .WithConfigFile("configs/radicale/config", "/etc/radicale/config", Path.Combine(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "config"))
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_CONFIG", "../services-data/system/radicale/config"), "/etc/radicale")
            .WithBindMount(settings.SmartHome.DataPath("RADICALE_DATA", "../services-data/system/radicale/data"), "/var/lib/radicale")
            .WithLabPort(5232, "radicale")
            .WithHomepage("Household", "Phonebook", "mdi-calendar-clock", href: $"https://phonebook.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("home-assistant", "ghcr.io/home-assistant/home-assistant", "stable")
            .WithContainerName("home-assistant")
            .WithStartPolicy(true)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.SmartHome.DataPath("HOME_ASSISTANT_DATA", "../services-data/system/home-assistant"), "/config")
            .WithConfigFile("configs/home-assistant/configuration.yaml", "/config/configuration.yaml")
            .WithBindMount("/etc/localtime", "/etc/localtime", isReadOnly: true)
            .WithBindMount("/run/dbus", "/run/dbus", isReadOnly: true)
            .WithHostNetwork()
            .WithLab(lab =>
            {
                lab.Privileged = true;
                lab.CapAdd.Add("NET_ADMIN");
                lab.CapAdd.Add("NET_RAW");
            })
            .WithHomepage("Household", "Home Assistant", "mdi-home-automation", href: $"https://home-assistant.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("homeassistant", "http://host.docker.internal:8123", key: HomepageVar("HOMEASSISTANT_WIDGET_KEY"));

        _ = builder.AddContainer("homebox", "sysadminsmedia/homebox", "latest")
            .WithStartPolicy(true)
            .WithContainerName("inventory")
            .WithEnvironment("HBOX_LOG_LEVEL", "info")
            .WithEnvironment("HBOX_LOG_FORMAT", "text")
            .WithBindMount(settings.SmartHome.DataPath("HOMEBOX_DATA", "../services-data/system/homebox"), "/data")
            .WithLabPort(7745, "homebox")
            .WithEnvironment("HBOX_AUTH_API_KEY_PEPPER", settings.Secret("HBOX_AUTH_API_KEY_PEPPER", SecretKind.Base64, Path.Combine(settings.SmartHome.DataPath("HOMEBOX_SECRETS", "../services-data/system/homebox/secrets"), "homebox_api_key_pepper")))
            .WithHomepage("Household", "Inventory", "mdi-package-variant-closed", href: $"https://inventory.{settings.Shared.DuckDnsDomain}")
            .WithHomepageWidget("homebox", "http://inventory:7745",
                username: HomepageVar("HOMEBOX_WIDGET_USERNAME"),
                password: HomepageVar("HOMEBOX_WIDGET_PASSWORD"));
    }
}
