using Jellyfin.Plugin.Turkanime.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Turkanime;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static Plugin Instance { get; private set; } = null!;

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override Guid Id => Guid.Parse("ebf4f4ac-6591-4926-9f43-9ec71d8d7f0b");

    public override string Name => "TürkAnime";

    public override string Description => "TürkAnime catalogue and external host playback integration for Jellyfin 12.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = "Turkanime.Page",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.turkanime.html",
                EnableInMainMenu = true,
                DisplayName = "TürkAnime",
                MenuSection = "main"
            },
            new PluginPageInfo
            {
                Name = "Turkanime.Style",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.turkanime.css"
            },
            new PluginPageInfo
            {
                Name = "Turkanime.Nav",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.plugin.js"
            }
        ];
    }
}
