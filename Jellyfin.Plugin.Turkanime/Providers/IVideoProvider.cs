using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Providers;

public interface IVideoProvider
{
    bool CanHandle(Uri url);

    Task<PlaybackInfo> ResolveAsync(Uri url, CancellationToken cancellationToken);
}
