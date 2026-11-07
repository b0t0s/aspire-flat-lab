using apphost.cs.src;
using static EnvironmentSettings;

internal static class DevelopmentServices
{
    public static void AddDevelopmentServices(this IDistributedApplicationBuilder builder, LabDependencies dependencies, EnvironmentSettings settings)
    {
        _ = builder.AddContainer("zerobyte", "ghcr.io/nicotsx/zerobyte", "v0.44")
            .WithContainerName("zerobyte")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithEnvironment("BASE_URL", $"https://backup.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("APP_SECRET", settings.Secret("ZEROBYTE_APP_SECRET", SecretKind.Hex32))
            .WithBindMount(settings.SmartHome.DataPath("ZEROBYTE_DATA", "../services-data/system/zerobyte"), "/var/lib/zerobyte")
            .WithBindMount(settings.Shared.DataRoot, "/mydata/services-data", isReadOnly: true)
            .WithBindMount(settings.SmartHome.DataPath("ZEROBYTE_REPOSITORIES", "../backups"), "/repositories")
            .WithLabPort(4096, "zerobyte")
            .WithHomepage("Infrastructure", "Backups", "mdi-backup-restore", "", $"https://backup.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("atuin", "ghcr.io/atuinsh/atuin", "latest")
            .WithContainerName("shell-history")
            .WithDefaultMode(LabMode.Auto)
            .WithArgs("start")
            .WithEnvironment("ATUIN_HOST", "0.0.0.0")
            .WithEnvironment("ATUIN_OPEN_REGISTRATION", "true")
            .WithEnvironment("ATUIN_DB_URI", ReferenceExpression.Create($"postgres://atuin:{settings.Secret("ATUIN_DB_PASSWORD")}@atuin-db:5432/atuin"))
            .WithBindMount(settings.Development.DataPath("ATUIN_CONFIG", "../services-data/system/atuin/config"), "/config")
            .WithLabPort(8888, "atuin")
            .WaitFor(dependencies.AtuinDb)
            .WithHomepage("Infrastructure", "Shell history", "mdi-console-line", "", $"https://shell-history.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("ideon", "ghcr.io/3xpyth0n/ideon", "latest")
            .WithContainerName("ideon")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("APP_URL", $"https://ideon.{settings.Shared.DuckDnsDomain}")
            .WithEnvironment("TIMEZONE", settings.Shared.TimeZone)
            .WithEnvironment("LOG_LEVEL", "info")
            .WithEnvironment("DB_HOST", "ideon-db")
            .WithEnvironment("DB_PORT", "5432")
            .WithEnvironment("DB_NAME", "ideon")
            .WithEnvironment("DB_USER", "ideon")
            .WithEnvironment("DB_PASS", settings.Secret("IDEON_DB_PASSWORD"))
            .WithEnvironment("SECRET_KEY", settings.Secret("IDEON_SECRET_KEY", SecretKind.Hex32))
            .WithBindMount(settings.Development.DataPath("IDEON_DATA", "../services-data/system/ideon"), "/app/storage")
            .WithLabPort(3000, "ideon")
            .WaitFor(dependencies.IdeonDb)
            .WithHomepage("Development", "Ideon", "mdi-drawing-box", "Visual workspace", $"https://ideon.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("onedev", "1dev/server", "latest")
            .WithContainerName("projects")
            .WithDefaultMode(LabMode.Auto)
            .WithBindMount(settings.Development.DataPath("ONEDEV_DATA", "../services-data/system/onedev"), "/opt/onedev")
            .WithDockerSocket(isReadOnly: false)
            .WithLabPort(6610, "onedev-http")
            .WithLabPort(6611, "onedev-agent")
            .WithHomepage("Development", "Projects", "mdi-git", href: $"https://projects.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("container-hub", "ghcr.io/eznix86/docker-registry-ui", "latest")
            .WithContainerName("registry")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("REGISTRY_URL", settings.Development.Value("REGISTRY_URL"))
            .WithEnvironment("REGISTRY_AUTH", settings.Development.Value("REGISTRY_AUTH"))
            .WithLabPort(3000, "container-hub")
            .WithHomepage("Infrastructure", "Registry UI", "mdi-docker", "Container Hub", $"https://registry.{settings.Shared.DuckDnsDomain}");

        var termixDataPath = settings.SmartHome.DataPath("TERMIX_DATA", "../services-data/system/termix");

        var guacd = builder.AddContainer("guacd", "guacamole/guacd", "1.6.0")
            .WithContainerName("guacd")
            .WithDefaultMode(LabMode.Auto)
            .WithBindMount(termixDataPath, "/termix-data");

        _ = builder.AddContainer("termix", "ghcr.io/termix-ssh/termix", "latest")
            .WithContainerName("termix")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("PORT", "8080")
            .WithEnvironment("GUACD_HOST", "guacd")
            .WithEnvironment("GUACD_TUNNEL_HOST", "termix")
            .WithEnvironment("GUACD_RECORDING_PATH", "/termix-data/session_recordings/guacamole")
            .WithEnvironment("GUACD_DRIVE_PATH", "/termix-data/rdp-drive")
            .WithBindMount(termixDataPath, "/app/data")
            .WithBindMount(termixDataPath, "/termix-data")
            .WithLabPort(8080, "termix")
            .WaitFor(guacd)
            .WithHomepage("Infrastructure", "SSH terminal", "mdi-console", "Termix web terminal", $"https://termix.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("opengist", "ghcr.io/thomiceli/opengist", "latest")
            .WithContainerName("snippets")
            .WithDefaultMode(LabMode.Auto)
            .WithEnvironment("OG_LISTEN_HOST", "0.0.0.0")
            .WithEnvironment("OG_LISTEN_PORT", "6157")
            .WithEnvironment("OG_REPOSITORY_PATH", "/app/repositories")
            .WithEnvironment("OG_SECRET_KEY", settings.Secret("OPENGIST_SECRET"))
            .WithBindMount(settings.Development.DataPath("OPENGIST_DATA", "../services-data/system/opengist"), "/opengist")
            .WithBindMount(settings.Development.DataPath("OPENGIST_REPOS", "../services-data/system/opengist-repos"), "/app/repositories")
            .WithLabPort(6157, "opengist")
            .WithHomepage("Development", "Snippets", "mdi-source-repository", href: $"https://snippets.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("jupyter", "quay.io/jupyter/scipy-notebook", "latest")
            .WithContainerName("jupyter")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("JUPYTER_TOKEN", settings.Secret("JUPYTER_TOKEN", SecretKind.Hex32))
            .WithEnvironment("NB_UID", settings.Shared.Uid)
            .WithEnvironment("NB_GID", settings.Shared.Gid)
            .WithEnvironment("CHOWN_HOME", "yes")
            .WithEnvironment("CHOWN_EXTRA", "/home/jovyan/work")
            .WithEnvironment("TZ", settings.Shared.TimeZone)
            .WithBindMount(settings.Knowledge.DataPath("JUPYTER_HOME", "../services-data/system/jupyter"), "/home/jovyan")
            .WithBindMount(settings.Knowledge.DataPath("JUPYTER_NOTEBOOKS", "../services-data/public/notebooks"), "/home/jovyan/work")
            .WithBindMount(settings.Shared.DataRoot + "/public", "/home/jovyan/public", isReadOnly: true)
            .WithLab(lab => lab.User = "root")
            .WithLabPort(8888, "jupyter")
            .WithHomepage("Development", "JupyterLab", "si-jupyter", "", $"https://notebooks.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("it-tools", "corentinth/it-tools", "latest")
            .WithContainerName("toolbox")
            .WithDefaultMode(LabMode.Auto)
            .WithLabPort(80, "it-tools")
            .WithHomepage("Development", "Toolbox", "mdi-tools", href: $"https://tools.{settings.Shared.DuckDnsDomain}");

        _ = builder.AddContainer("convertx", "ghcr.io/c4illin/convertx", "latest")
            .WithContainerName("converter")
            .WithDefaultMode(LabMode.Manual)
            .WithEnvironment("JWT_SECRET", settings.Secret("CONVERTX_JWT_SECRET"))
            .WithBindMount(settings.Development.DataPath("CONVERTX_DATA", "../services-data/system/convertx"), "/app/data")
            .WithLabPort(3000, "convertx")
            .WithHomepage("Development", "Converter", "mdi-file-convert", href: $"https://convert.{settings.Shared.DuckDnsDomain}");
    }
}
