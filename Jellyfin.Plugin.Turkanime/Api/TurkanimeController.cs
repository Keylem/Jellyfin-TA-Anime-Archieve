using Jellyfin.Plugin.Turkanime.Models;
using Jellyfin.Plugin.Turkanime.Providers;
using Jellyfin.Plugin.Turkanime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Turkanime.Api;

[ApiController]
[Route("Plugins/Turkanime")]
public sealed class TurkanimeController : ControllerBase
{
    private static readonly CatalogService Catalog = new(
        configurationDirectoryPath: Path.Combine(AppContext.BaseDirectory, "plugins", "Turkanime"));

    [HttpGet("Anime")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Anime>>> GetAnime(CancellationToken cancellationToken)
    {
        var anime = await Catalog.GetAnimeAsync(cancellationToken).ConfigureAwait(false);
        return Ok(anime);
    }

    [HttpGet("Anime/{id}/Episodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Episode>>> GetEpisodes([FromRoute] string id, CancellationToken cancellationToken)
    {
        var episodes = await Catalog.GetEpisodesAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(episodes);
    }

    [HttpGet("Resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaybackInfo>> Resolve([FromQuery] string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return BadRequest("Invalid url query string value.");
        }

        var result = await VideoProviderResolver.ResolveAsync(uri, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
