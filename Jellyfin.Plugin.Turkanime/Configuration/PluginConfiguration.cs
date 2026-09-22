using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Turkanime.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string CatalogFileName { get; set; } = "anime.json";
}
