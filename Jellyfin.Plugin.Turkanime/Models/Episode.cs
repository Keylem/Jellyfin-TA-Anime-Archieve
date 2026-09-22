namespace Jellyfin.Plugin.Turkanime.Models;

public sealed class Episode
{
    public string Id { get; set; } = string.Empty;

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Provider { get; set; } = "external";

    public string Url { get; set; } = string.Empty;
}
