# Jellyfin-TA-Anime-Archieve

Minimal Jellyfin 12-oriented TürkAnime plugin scaffold.

## Project layout

- `/Jellyfin.Plugin.Turkanime/Api/TurkanimeController.cs`
  - `GET /Plugins/Turkanime/Anime`
  - `GET /Plugins/Turkanime/Anime/{id}/Episodes`
  - `GET /Plugins/Turkanime/Resolve?url=...`
- `/Jellyfin.Plugin.Turkanime/Providers/MailRuProvider.cs`
  - Provider abstraction for Mail.ru URL handling
- `/Jellyfin.Plugin.Turkanime/Providers/VideoProviderResolver.cs`
  - Dynamic provider detection from episode URLs
- `/Jellyfin.Plugin.Turkanime/Web/`
  - Searchable TürkAnime page and playback routing (`native` / `embed` / `external`)

## Catalogue source

The plugin follows the `turkanime-indirici` flow:

- Reads manifest from:
  - `https://raw.githubusercontent.com/KebabLord/turkanime-indirici/refs/heads/master/manifest.json`
- Uses `animedepo_url` from that manifest (default:
  - `https://gitlab.com/AnimeDepo/animedepo/-/raw/master`)
- Fetches anime list from `dizin.json`
- Fetches episode list from `animeler/{slug}/bolumler.json`
- Fetches episode sources from `animeler/{slug}/{episodeSlug}.json`

If remote fetch fails, it falls back to local `anime.json` in the plugin configuration directory.

`provider` is resolved dynamically from each episode URL.

Supported source detection:
- Sibnet
- Odnoklassinki
- Myvi
- Sendvid
- Mail.ru
- MP4upload
- Vidmoly
- Dailymotion
- Yandisk
- Uqload
- Drive
- VK
