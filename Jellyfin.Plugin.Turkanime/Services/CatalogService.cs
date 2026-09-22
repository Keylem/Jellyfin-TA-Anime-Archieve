using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Turkanime.Models;
using Jellyfin.Plugin.Turkanime.Providers;

namespace Jellyfin.Plugin.Turkanime.Services;

public sealed class CatalogService
{
    private const string ManifestUrl = "https://raw.githubusercontent.com/KebabLord/turkanime-indirici/refs/heads/master/manifest.json";
    private const string DefaultAnimeDepoUrl = "https://gitlab.com/AnimeDepo/animedepo/-/raw/master";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static readonly HttpClient HttpClient = new();

    private readonly string _catalogPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CatalogService(string configurationDirectoryPath, string fileName = "anime.json")
    {
        _catalogPath = Path.Combine(configurationDirectoryPath, fileName);
    }

    public async Task<IReadOnlyList<Anime>> GetAnimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifest = await GetManifestSettingsAsync(cancellationToken).ConfigureAwait(false);
            var dizin = await FetchJsonAsync(manifest.AnimeDepoUrl, "dizin.json", cancellationToken).ConfigureAwait(false);
            var anime = BuildAnimeFromIndex(dizin);

            return anime;
        }
        catch
        {
            var catalog = await LoadLocalCatalogAsync(cancellationToken).ConfigureAwait(false);
            return catalog.Anime;
        }
    }

    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken)
    {
        try
        {
            var manifest = await GetManifestSettingsAsync(cancellationToken).ConfigureAwait(false);
            var bolumler = await FetchJsonAsync(manifest.AnimeDepoUrl, $"animeler/{animeId}/bolumler.json", cancellationToken).ConfigureAwait(false);
            return await BuildEpisodesAsync(
                animeId,
                bolumler,
                manifest.AnimeDepoUrl,
                manifest.ForceFallback,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            var catalog = await LoadLocalCatalogAsync(cancellationToken).ConfigureAwait(false);
            var anime = catalog.Anime.FirstOrDefault(x => string.Equals(x.Id, animeId, StringComparison.OrdinalIgnoreCase));
            return anime?.Episodes ?? Array.Empty<Episode>();
        }
    }

    private async Task<CatalogDocument> LoadLocalCatalogAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(_catalogPath))
            {
                var directory = Path.GetDirectoryName(_catalogPath)!;
                Directory.CreateDirectory(directory);

                var seed = CreateSeedCatalog();
                NormalizeCatalog(seed);
                var seedJson = JsonSerializer.Serialize(seed, SerializerOptions);
                await File.WriteAllTextAsync(_catalogPath, seedJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                return seed;
            }

            var json = await File.ReadAllTextAsync(_catalogPath, cancellationToken).ConfigureAwait(false);
            var catalog = JsonSerializer.Deserialize<CatalogDocument>(json, SerializerOptions) ?? new CatalogDocument();
            NormalizeCatalog(catalog);

            return catalog;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<ManifestSettings> GetManifestSettingsAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ManifestUrl);
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        var animeDepoUrl = root.TryGetProperty("animedepo_url", out var animeDepoElement) &&
            animeDepoElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(animeDepoElement.GetString())
            ? animeDepoElement.GetString()!
            : DefaultAnimeDepoUrl;

        var forceFallback = root.TryGetProperty("features", out var features) &&
            features.TryGetProperty("search", out var search) &&
            search.TryGetProperty("force_fallback", out var forceFallbackElement) &&
            forceFallbackElement.ValueKind == JsonValueKind.True;

        return new ManifestSettings
        {
            AnimeDepoUrl = animeDepoUrl.TrimEnd('/'),
            ForceFallback = forceFallback
        };
    }

    private static async Task<JsonElement> FetchJsonAsync(string baseUrl, string path, CancellationToken cancellationToken)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.Clone();
    }

    private static IReadOnlyList<Anime> BuildAnimeFromIndex(JsonElement dizin)
    {
        var anime = new List<Anime>();

        if (!dizin.TryGetProperty("index", out var index) || index.ValueKind != JsonValueKind.Object)
        {
            return anime;
        }

        foreach (var group in index.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var entry in group.Value.EnumerateObject())
            {
                var slug = entry.Name;
                var title = entry.Value.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String
                    ? titleElement.GetString() ?? slug
                    : slug;
                var poster = entry.Value.TryGetProperty("poster", out var posterElement) && posterElement.ValueKind == JsonValueKind.String
                    ? posterElement.GetString()
                    : null;

                anime.Add(new Anime
                {
                    Id = slug,
                    Title = title,
                    Poster = poster,
                    Episodes = Array.Empty<Episode>()
                });
            }
        }

        return anime
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<IReadOnlyList<Episode>> BuildEpisodesAsync(
        string animeId,
        JsonElement bolumler,
        string animeDepoUrl,
        bool forceFallback,
        CancellationToken cancellationToken)
    {
        var episodes = new List<Episode>();
        if (bolumler.ValueKind != JsonValueKind.Array)
        {
            return episodes;
        }

        var number = 1;
        foreach (var entry in bolumler.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() < 2)
            {
                continue;
            }

            var episodeSlug = entry[0].GetString();
            var episodeTitle = entry[1].GetString();
            if (string.IsNullOrWhiteSpace(episodeSlug))
            {
                continue;
            }

            var episodeUrl = await TryResolveEpisodeUrlAsync(
                animeDepoUrl,
                animeId,
                episodeSlug,
                forceFallback,
                cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(episodeUrl))
            {
                continue;
            }

            episodes.Add(new Episode
            {
                Id = episodeSlug,
                Number = number++,
                Title = string.IsNullOrWhiteSpace(episodeTitle) ? episodeSlug : episodeTitle,
                Provider = VideoProviderResolver.DetectProvider(episodeUrl),
                Url = episodeUrl
            });
        }

        return episodes;
    }

    private static async Task<string?> TryResolveEpisodeUrlAsync(
        string animeDepoUrl,
        string animeId,
        string episodeSlug,
        bool forceFallback,
        CancellationToken cancellationToken)
    {
        var detail = await FetchJsonAsync(animeDepoUrl, $"animeler/{animeId}/{episodeSlug}.json", cancellationToken).ConfigureAwait(false);
        if (detail.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var source in detail.EnumerateArray())
        {
            if (source.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (source.TryGetProperty("alive", out var aliveElement) && aliveElement.ValueKind == JsonValueKind.False)
            {
                continue;
            }

            if (forceFallback && source.TryGetProperty("url", out var fallbackUrlElement))
            {
                if (fallbackUrlElement.ValueKind == JsonValueKind.String &&
                    Uri.TryCreate(fallbackUrlElement.GetString(), UriKind.Absolute, out var fallbackUrl))
                {
                    return fallbackUrl.ToString();
                }

                continue;
            }

            foreach (var key in new[] { "url", "mask", "path" })
            {
                if (!source.TryGetProperty(key, out var candidate) || candidate.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (Uri.TryCreate(candidate.GetString(), UriKind.Absolute, out var url))
                {
                    return url.ToString();
                }
            }
        }

        return null;
    }

    private static CatalogDocument CreateSeedCatalog()
    {
        return new CatalogDocument
        {
            Anime =
            [
                new Anime
                {
                    Id = "one-piece",
                    Title = "One Piece",
                    Episodes =
                    [
                        new Episode
                        {
                            Id = "ep-1",
                            Number = 1,
                            Title = "Episode 1",
                            Url = "https://my.mail.ru/video/embed/example/episode1"
                        }
                    ]
                },
                new Anime
                {
                    Id = "naruto",
                    Title = "Naruto",
                    Episodes =
                    [
                        new Episode
                        {
                            Id = "ep-1",
                            Number = 1,
                            Title = "Episode 1",
                            Url = "https://my.mail.ru/video/embed/example/naruto1"
                        }
                    ]
                }
            ]
        };
    }

    private static void NormalizeCatalog(CatalogDocument catalog)
    {
        foreach (var anime in catalog.Anime)
        {
            if (string.IsNullOrWhiteSpace(anime.Id))
            {
                anime.Id = Slugify(anime.Title);
            }

            foreach (var episode in anime.Episodes)
            {
                if (string.IsNullOrWhiteSpace(episode.Id))
                {
                    episode.Id = $"ep-{episode.Number}";
                }

                episode.Provider = VideoProviderResolver.DetectProvider(episode.Url);
            }
        }
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "anime";
        }

        return string.Join('-', value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed class ManifestSettings
    {
        public string AnimeDepoUrl { get; init; } = DefaultAnimeDepoUrl;

        public bool ForceFallback { get; init; }
    }
}
