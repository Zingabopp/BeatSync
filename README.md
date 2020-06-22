<img src="https://github.com/Zingabopp/BeatSync/blob/master/BeatSyncLib/Icons/BeatSyncLogoBanner.png" height="150">

Beat Saber plugin to automatically download songs.
The following sources and feeds are currently supported:
 * Beast Saber: Bookmarks, Follows, Curator Recommended
 * Beat Saver: FavoriteMappers, Hot, Downloads, Latest
 * ScoreSaber: Trending, Top Ranked, Latest Ranked, Top Played
# Installation (BeatSyncConsole)
BeatSyncConsole is a standalone application that runs separately from Beat Saber (and doesn't even need Beat Saber installed).
* Download BeatSyncConsole.zip from the [Releases](https://github.com/Zingabopp/BeatSync/releases) page.
  * Windows users should use the zip that includes `_win_x64` in the name.
  * Not all releases may include BeatSyncConsole.zip.
* Extract the zip file to a folder on your PC.
  * It is recommended to extract to a folder in your user folder to avoid NTFS file permissions problems that can occur if you extract to a folder like `Program Files` or `Program Files (x86)`.
## Usage
* Run `BeatSyncConsole.exe`.
* On Windows, if you don't have an existing config it will ask if you want it to search your PC for Beat Saber installations.
  * Type `Y` and press `Enter` to automatically add Beat Saber installations to your config.
  * If Beat Saber installations are found:
    * If a single installation is found, it is automatically enabled and it will proceed with the default feeds.
    * If multiple installations are found, it will ask which installations you want to enable in BeatSync.
* The config files `BeatSyncConsole.json` and `BeatSync.json` will be generated in the `configs` folder.
  * `BeatSyncConsole.json` contains settings specific to the console app, such as target locations to download songs to.
    * The `BeatSyncConfigPath` can be used to use an alternate `BeatSync.json` file (such as the one located in your `Beat Saber\UserData` folder.
    * **Important**: For JSON strings `\` is a special character. If you manually add a path, make sure you separate directories using `\\`. i.e. `C:\\Program Files\\Steam`.
  * `BeatSync.json` contains the feed configurations and is shared between BeatSyncConsole and the BeatSync mod. You can view details for the feed configuration [Here](#feed-configuration).
  * Once configured, run `BeatSyncConsole.exe` any time you want to download new songs.
## Have Issues?
* BeatSyncConsole stores a log file from the last run in the `logs` folder.
* Let me know of issues by creating an [Issue](https://github.com/Zingabopp/BeatSync/issues) or Ping/DM me in the [BeatSaberModdingGroup](https://discord.gg/beatsabermods) Discord server @Zingabopp#6764
  * Please include your log file.
# Installation (BeatSync mod, not released yet)
* Extract the release zip to your Beat Saber folder (or manually place BeatSync.dll into your Plugins folder).
* You must also have the following mods for BeatSync to function:
  * BSIPA 
  * SongCore
  * BeatSaberMarkupLanguage
  * BeatSaverDownloader

## Usage
**First Run:**
* Run Beat Saber after installing the mod and configure the settings in-game (Settings > BeatSync) or in UserData\BeatSync.json.
* Beat Saber must be restarted (or disabled and reenabled via BSIPA ModList) for it to use the modified settings to download songs.

# Feed Configuration
Configuration can be done in-game in Settings > BeatSync or by (carefully) editing the BeatSync.json file in your UserData folder.

__Each feed has the following options:__
* Enabled: If true, BeatSync will use this feed to download songs.
* MaxSongs: Maximum number of songs to download from that feed. Set to 0 for unlimited.
* CreatePlaylist: Creates/Maintains a playlist using songs from the feed. Songs that are found by the feed but have already been downloaded are also added to the playlist.
* PlaylistStyle: If set to 'Append', each session will add the songs it finds to the playlist. If set to 'Replace', the playlist will only contain songs that were found in the current session (useful for ScoreSaber's ranked feeds so songs that get unranked are removed).

__BeatSync.json__
* ~~RegenerateConfig: If true, set all options to defaults on next game start.~~
* DownloadTimeout: How long to wait for a download response before giving up.
* MaxConcurrentDownloads: How many queued songs to download at the same time, higher is not always better - use a reasonable value.
* RecentPlaylistDays: How long recently downloaded songs stay in the BeatSync Recent playlist. (0 to disable the playlist)
* AllBeatSyncSongsPlaylist: If enabled, BeatSync creates a playlist with all the songs it finds (large playlists can cause a big freeze after custom songs are initially loaded)
* BeastSaber:
  * Enabled: If false, disables all feeds from this source.
  * Username: Your BeastSaber username. You must enter this to get songs from the Bookmarks and Follows feeds.
  * MaxConcurrentPageChecks: Number of pages to check simultaneously when reading feeds.
  * Bookmarks: Get bookmarked songs from BeastSaber.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
  * Follows: Get songs from mappers you are following on BeastSaber.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
  * CuratorRecommended: Get songs from BeastSaber's Curator Recommended list.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
* BeatSaver
  * Enabled: If false, disables all feeds from this source.
  * MaxConcurrentPageChecks: Number of pages to check simultaneously when reading feeds.
  * FavoriteMappers: Feed that downloads songs from each mapper specified in FavoriteMappers.ini in the UserData folder.
    * Enabled
    * MaxSongs: Maximum number of songs to download from each mapper.
    * CreatePlaylist
    * PlaylistStyle
    * SeparateMapperPlaylists: If true (along with CreatePlaylist), create a playlist for each mapper in your FavoriteMappers.ini file.
  * Hot: Get songs using Beat Saver's "Hot" ordering.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver.**
    * CreatePlaylist
    * PlaylistStyle
  * Downloads: Get songs by most downloaded from Beat Saver
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver.**
    * CreatePlaylist
    * PlaylistStyle
* ScoreSaber
  * Enabled: If false, disables all feeds from this source.
  * TopRanked: Get ranked songs by ScoreSaber's "Star" rating.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked songs get removed.
    * IncludeUnstarred: Include songs that don't have a star rating (no affect on this feed, all songs will have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (songs with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * LatestRanked: Get ScoreSaber's ranked songs, most recently ranked first (more or less).
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked songs get removed.
    * IncludeUnstarred: Include songs that don't have a star rating (no affect on this feed, all songs will have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (songs with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * Trending: Get songs using ScoreSaber's "Trending" ordering.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver if you don't have RankedOnly enabled.**
    * CreatePlaylist
    * PlaylistStyle
    * RankedOnly: If true, only get ranked songs.
    * IncludeUnstarred: Include songs that don't have a star rating (unranked songs can still have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (songs with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * TopPlayed: Get the most played songs as reported by ScoreSaber.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver if you don't have RankedOnly enabled.**
    * CreatePlaylist
    * PlaylistStyle
    * RankedOnly: If true, only get ranked songs.
    * IncludeUnstarred: Include songs that don't have a star rating (unranked songs can still have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (songs with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
	
# Additional Information
* BeatSync maintains a history file for songs it finds in the feeds. This is used to prevent BeatSync from redownloading songs you've deleted. It is sorted in descending order by date the song was added to the history. This file can safely be deleted (or carefully edited) if you want BeatSync to download songs you've deleted in the past.
* If you use the SongBrowser plugin, it will not show the playlists in the correct order (i.e. TopRanked and LatestRanked will be out of order).

# Credits
* Brian for the original SyncSaber mod.
* JW#1944 (Discord) for the fancy logo.
