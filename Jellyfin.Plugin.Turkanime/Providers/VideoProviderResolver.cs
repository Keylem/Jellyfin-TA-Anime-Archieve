using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Providers;

public static class VideoProviderResolver
{
    private static readonly IVideoProvider[] Providers =
    [
        new MailRuProvider(),
        new GenericHostProvider("sibnet", ["sibnet.ru"]),
        new GenericHostProvider("odnoklassniki", ["ok.ru", "odnoklassniki.ru"], TransformOdnoklassnikiEmbed),
        new GenericHostProvider("myvi", ["myvi.tv"]),
        new GenericHostProvider("sendvid", ["sendvid.com"]),
        new GenericHostProvider("mp4upload", ["mp4upload.com"]),
        new GenericHostProvider("vidmoly", ["vidmoly"]),
        new GenericHostProvider("dailymotion", ["dailymotion.com", "dai.ly"], TransformDailymotionEmbed),
        new GenericHostProvider("yandisk", ["disk.yandex", "yadi.sk"]),
        new GenericHostProvider("uqload", ["uqload"]),
        new GenericHostProvider("drive", ["drive.google.com"], TransformDriveEmbed),
        new GenericHostProvider("vk", ["vk.com", "vkvideo.ru"])
    ];

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

    private static Uri? TransformDailymotionEmbed(Uri url)
    {
        if (!url.Host.Contains("dailymotion.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!url.AbsolutePath.StartsWith("/video/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var embedPath = "/embed" + url.AbsolutePath;
        return new UriBuilder(url) { Path = embedPath }.Uri;
    }

    private static Uri? TransformOdnoklassnikiEmbed(Uri url)
    {
        if (!url.AbsolutePath.StartsWith("/video/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var id = url.AbsolutePath["/video/".Length..].Trim('/');
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new Uri($"https://ok.ru/videoembed/{id}");
    }

    private static Uri? TransformDriveEmbed(Uri url)
    {
        if (!url.AbsolutePath.StartsWith("/file/d/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return null;
        }

        var fileId = parts[2];
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        return new Uri($"https://drive.google.com/file/d/{fileId}/preview");
    }
}
