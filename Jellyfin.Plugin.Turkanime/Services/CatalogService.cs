using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Turkanime.Models;

namespace Jellyfin.Plugin.Turkanime.Services;

public sealed class CatalogService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _catalogPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CatalogService(string configurationDirectoryPath, string fileName = "anime.json")
    {
        _catalogPath = Path.Combine(configurationDirectoryPath, fileName);
    }

    public async Task<IReadOnlyList<Anime>> GetAnimeAsync(CancellationToken cancellationToken)
    {
        var catalog = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return catalog.Anime;
    }

    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(string animeId, CancellationToken cancellationToken)
    {
        var catalog = await LoadAsync(cancellationToken).ConfigureAwait(false);
        var anime = catalog.Anime.FirstOrDefault(x => string.Equals(x.Id, animeId, StringComparison.OrdinalIgnoreCase));
        return anime?.Episodes ?? Array.Empty<Episode>();
    }

    private async Task<CatalogDocument> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(_catalogPath))
            {
                var directory = Path.GetDirectoryName(_catalogPath)!;
                Directory.CreateDirectory(directory);

                var seed = CreateSeedCatalog();
                var seedJson = JsonSerializer.Serialize(seed, SerializerOptions);
                await File.WriteAllTextAsync(_catalogPath, seedJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                return seed;
            }

            var json = await File.ReadAllTextAsync(_catalogPath, cancellationToken).ConfigureAwait(false);
            var catalog = JsonSerializer.Deserialize<CatalogDocument>(json, SerializerOptions) ?? new CatalogDocument();

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
                }
            }

            return catalog;
        }
        finally
        {
            _gate.Release();
        }
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
                            Provider = "mailru",
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
                            Provider = "mailru",
                            Url = "https://my.mail.ru/video/embed/example/naruto1"
                        }
                    ]
                }
            ]
        };
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
}
