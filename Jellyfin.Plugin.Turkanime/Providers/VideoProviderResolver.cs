using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Providers;

public static class VideoProviderResolver
{
    private static readonly IVideoProvider[] Providers = [new MailRuProvider()];

    public static string DetectProvider(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "external";
        }

        var provider = Providers.FirstOrDefault(x => x.CanHandle(uri));
        return provider?.Id ?? "external";
    }

    public static async Task<PlaybackInfo> ResolveAsync(Uri uri, CancellationToken cancellationToken)
    {
        var provider = Providers.FirstOrDefault(x => x.CanHandle(uri));
        if (provider is null)
        {
            return new PlaybackInfo
            {
                Provider = "external",
                Mode = "external",
                Url = uri.ToString()
            };
        }

        return await provider.ResolveAsync(uri, cancellationToken).ConfigureAwait(false);
    }
}
