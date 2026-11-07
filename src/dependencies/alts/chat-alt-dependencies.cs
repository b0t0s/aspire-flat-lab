namespace apphost.cs.src;

internal sealed partial class LabDependencies
{
    public IResourceBuilder<ContainerResource> Ollama => Once(() => Add("ollama", "ollama/ollama", "latest")
        .WithEnvironment("OLLAMA_ORIGINS", "*")
        .WithEnvironment("OLLAMA_HOST", "0.0.0.0")
        .WithEnvironment("OLLAMA_KEEP_ALIVE", "24h")
        .WithEnvironment("OLLAMA_API_KEY", Settings.Chat.Value("OLLAMA_API_KEY"))
        .WithBindMount(Settings.Chat.DataPath("OLLAMA_DATA", "../services-data/system/ollama"), "/root/.ollama")
        .WithPublishedPort(11434, 11434));

    public IResourceBuilder<ContainerResource> SynapseDb => Once(() => Postgres("synapse-db", "16-alpine", "synapse", "synapse", "SYNAPSE_DB_PASSWORD",
            Settings.Chat.DataPath("SYNAPSE_DB_DATA", "../services-data/system/synapse/postgres"))
        .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8 --lc-collate=C --lc-ctype=C"));

    public IResourceBuilder<ContainerResource> LemmyDb => Once(() => Postgres("lemmy-db", "16-alpine", "lemmy", "lemmy", "LEMMY_DB_PASSWORD",
        Settings.Chat.DataPath("LEMMY_DB_DATA", "../services-data/system/lemmy/postgres")));

    public IResourceBuilder<ContainerResource> LemmyProjectPictrs => Once(() => Add("lemmy-project-pictrs", "asonix/pictrs", "0.5.24")
        .WithContainerNetworkAlias("pictrs")
        .WithEnvironment("PICTRS__SERVER__API_KEY", Settings.Secret("LEMMY_PICTRS_API_KEY", aliases: "LEMMY_DB_PASSWORD"))
        .WithEnvironment("RUST_BACKTRACE", "full")
        .WithEnvironment("PICTRS__MEDIA__VIDEO__VIDEO_CODEC", "vp9")
        .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_WIDTH", "256")
        .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_HEIGHT", "256")
        .WithEnvironment("PICTRS__MEDIA__ANIMATION__MAX_FRAME_COUNT", "400")
        .WithBindMount(Path.Combine(Settings.Chat.DataPath("LEMMY_PROJECT_DATA", "../services-data/system/lemmy-project"), "pictrs"), "/mnt")
        .WithLab(lab => lab.User = "991:991"));
}
