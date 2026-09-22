namespace Jellyfin.Plugin.Turkanime;

public sealed class Plugin
{
    public Plugin(string configurationDirectoryPath)
    {
        ConfigurationDirectoryPath = configurationDirectoryPath;
    }

    public static Plugin? Instance { get; private set; }

    public string ConfigurationDirectoryPath { get; }

    public static Plugin Initialize(string configurationDirectoryPath)
    {
        return Instance ??= new Plugin(configurationDirectoryPath);
    }
}
