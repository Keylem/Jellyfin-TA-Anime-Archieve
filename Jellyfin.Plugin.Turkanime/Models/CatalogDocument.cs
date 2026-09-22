namespace Jellyfin.Plugin.Turkanime.Models;

public sealed class CatalogDocument
{
    public List<Anime> Anime { get; set; } = new();
}
