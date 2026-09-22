namespace Jellyfin.Plugin.Turkanime.Models;

public sealed class Anime
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Poster { get; set; }

    public IReadOnlyList<Episode> Episodes { get; set; } = Array.Empty<Episode>();
}
