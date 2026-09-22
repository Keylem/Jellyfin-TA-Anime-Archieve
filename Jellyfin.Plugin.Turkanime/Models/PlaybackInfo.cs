namespace Jellyfin.Plugin.Turkanime.Models;

public sealed class PlaybackInfo
{
    public string Mode { get; set; } = "external";

    public string Provider { get; set; } = "external";

    public string Url { get; set; } = string.Empty;
}
