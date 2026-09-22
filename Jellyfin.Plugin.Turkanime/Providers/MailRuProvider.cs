using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Providers;

public sealed class MailRuProvider : IVideoProvider
{
    private static readonly string[] NativeExtensions = [".m3u8", ".mpd", ".mp4"];

    public bool CanHandle(Uri url)
    {
        return url.Host.Contains("mail.ru", StringComparison.OrdinalIgnoreCase);
    }

    public Task<PlaybackInfo> ResolveAsync(Uri url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (NativeExtensions.Any(ext => url.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(new PlaybackInfo
            {
                Provider = "mailru",
                Mode = "native",
                Url = url.ToString()
            });
        }

        if (url.AbsolutePath.Contains("/video/embed/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PlaybackInfo
            {
                Provider = "mailru",
                Mode = "embed",
                Url = url.ToString()
            });
        }

        if (url.AbsolutePath.StartsWith("/video/", StringComparison.OrdinalIgnoreCase))
        {
            var embedPath = url.AbsolutePath.Replace("/video/", "/video/embed/", StringComparison.OrdinalIgnoreCase);
            var embedUri = new UriBuilder(url) { Path = embedPath }.Uri;

            return Task.FromResult(new PlaybackInfo
            {
                Provider = "mailru",
                Mode = "embed",
                Url = embedUri.ToString()
            });
        }

        return Task.FromResult(new PlaybackInfo
        {
            Provider = "mailru",
            Mode = "external",
            Url = url.ToString()
        });
    }
}
