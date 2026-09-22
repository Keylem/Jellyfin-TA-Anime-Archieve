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

## Catalogue JSON

The plugin persists catalogue data in a JSON file named `anime.json` in the plugin configuration directory.

```json
{
  "anime": [
    {
      "id": "one-piece",
      "title": "One Piece",
      "episodes": [
        {
          "id": "ep-1",
          "number": 1,
          "title": "Episode 1",
          "url": "https://my.mail.ru/video/embed/example/episode1"
        }
      ]
    }
  ]
}
```

`provider` is resolved dynamically from each episode `url` during catalog load.
