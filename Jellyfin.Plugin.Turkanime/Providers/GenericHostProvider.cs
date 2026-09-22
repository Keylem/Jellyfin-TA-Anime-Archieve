using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Providers;

public sealed class GenericHostProvider : IVideoProvider
{
    private static readonly string[] NativeExtensions = [".m3u8", ".mpd", ".mp4", ".webm", ".mkv"];
    private readonly string[] _hostMarkers;
    private readonly Func<Uri, Uri?>? _embedTransform;

    public GenericHostProvider(string id, string[] hostMarkers, Func<Uri, Uri?>? embedTransform = null)
    {
        Id = id;
        _hostMarkers = hostMarkers;
        _embedTransform = embedTransform;
    }

    public string Id { get; }

    public bool CanHandle(Uri url)
    {
        return _hostMarkers.Any(marker => url.Host.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public Task<PlaybackInfo> ResolveAsync(Uri url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (NativeExtensions.Any(ext => url.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(new PlaybackInfo
            {
                Provider = Id,
                Mode = "native",
                Url = url.ToString()
            });
        }

        var transformedEmbed = _embedTransform?.Invoke(url);
        if (transformedEmbed is not null)
        {
            return Task.FromResult(new PlaybackInfo
            {
                Provider = Id,
                Mode = "embed",
                Url = transformedEmbed.ToString()
            });
        }

        if (url.AbsolutePath.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
            url.Query.Contains("embed", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PlaybackInfo
            {
                Provider = Id,
                Mode = "embed",
                Url = url.ToString()
            });
        }

        return Task.FromResult(new PlaybackInfo
        {
            Provider = Id,
            Mode = "external",
            Url = url.ToString()
        });
    }
}
