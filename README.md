## IMPORTANT: As announced in the Beat Saber Modding Group Discord server on Aug. 2nd, 2021, ownership of Beat Saver has been changed from the Jellyfish to Top_Cat. Along with the handover, there were some major changes to the site and API. This is a breaking change for anything using Beat Saver. <br><br>A beta build has been posted in [Releases](https://github.com/Zingabopp/BeatSync/releases).

<img src="https://github.com/Zingabopp/BeatSync/blob/master/BeatSyncLib/Icons/BeatSyncLogoBanner.png" height="150">

Beat Saber plugin to automatically download beatmaps.
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
  * Optional command line arguments:
    * `-c <PATH>`: Path to the directory containing `BeatSyncConsole.json`. Use this if you want to use a different location for the config file.
    * `-L <PATH>`: Path to the directory to store BeatSyncConsole's log file in.
* If you don't have an existing config it will ask if you want it to search your PC for Beat Saber installations.
  * Type `Y` and press `Enter` to automatically add Beat Saber installations to your config.
  * If Beat Saber installations are found:
    * If a single installation is found, it is automatically enabled and it will proceed with the default feeds.
    * If multiple installations are found, it will ask which installations you want to enable in BeatSync.
* The config files `BeatSyncConsole.json` and `BeatSync.json` will be generated in the `configs` folder.
  * `BeatSyncConsole.json` contains settings specific to the console app, such as target locations to download beatmaps to.
    * The `BeatSyncConfigPath` can be used to use an alternate `BeatSync.json` file (such as the one located in your `Beat Saber\UserData` folder.
    * **Important**: For JSON strings `\` is a special character. If you manually add a path, make sure you separate directories using `\\`. i.e. `C:\\Program Files\\Steam`.
  * `BeatSync.json` contains the feed configurations and is shared between BeatSyncConsole and the BeatSync mod. You can view details for the feed configuration [Here](#feed-configuration).
  * Once configured, run `BeatSyncConsole.exe` any time you want to download new beatmaps.
## Configuration (BeatSyncConsole.json)
* BeatSyncConfigPath: Path to the `BeatSync.json` config file you want to use. By default this is set to `%CONFIG%\BeatSync.json` which is in the `configs` folder in your `BeatSyncConsole` directory. This can be changed to point to the one in your `Beat Saber\UserData` folder so that BeatSyncConsole uses the same feed config as the BeatSync mod. `%CONFIG%` will be substituted for the directory `BeatSyncConsole.json` is read from.
* BeatSaberInstallLocations: This is a list of game install locations you want BeatSync to download beatmaps to. Each location has two properties:
  * Enabled: Whether or not this location is enabled.
  * GameDirectory: Path to your `Beat Saber` directory.
* AlternateSongPaths: This is a list of locations you want beatmaps downloaded to that are *not* game install locations.
  * Enabled: Whether or not this location is enabled.
  * BasePath: Base directory of the location. Relative paths will start at this directory.
  * SongsDirectory: Location the beatmaps are downloaded to.
  * PlaylistDirectory: Location the playlists are stored.
  * HistoryPath: Path to the history file, must include the file name.
  * UnzipBeatmaps: If true, downloaded beatmaps will be unzipped to the songs directory. If false, beatmaps will be copied as-is to the `SongsDirectory`.
* Special path substitutes: These variables can be used at the **start** of any path in `BeatSyncConsole.json`.
  * `%WORKINGDIR%`: The current working directory.
  * `%ASSEMBLYDIR%`: The directory of the BeatSyncConsole executable.
  * `%USER%` or `~`: The user's home folder.
  * `%TEMP%`: The temporary directory used by BeatSyncConsole.
* CloseWhenFinished: If true, close the console window after finishing instead of waiting for keyboard input.
* UseSystemTemp: If true, BeatSyncConsole will use the system's temporary folder (beatmaps will be downloaded here before they are distributed to their destinations). If false, a `Temp` folder will be created in the same directory as the BeatSyncConsole executable.
* ConsoleLogLevel: Log message level to display in the console, default is `Info`. Options are `Debug`, `Info`, `Warn`, `Critical`, `Error`, `Disabled`.
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
* Beat Saber must be restarted (or disabled and reenabled via BSIPA ModList) for it to use the modified settings to download beatmaps.

# Feed Configuration
Configuration can be done in-game in Settings > BeatSync or by (carefully) editing the BeatSync.json file in your UserData folder.

__Each feed has the following options:__
* Enabled: If true, BeatSync will use this feed to download beatmaps.
* MaxSongs: Maximum number of songs to download from that feed. Set to 0 for unlimited.
* CreatePlaylist: Creates/Maintains a playlist using beatmaps from the feed. Beatmaps that are found by the feed but have already been downloaded are also added to the playlist.
* PlaylistStyle: If set to 'Append', each session will add the beatmaps it finds to the playlist. If set to 'Replace', the playlist will only contain beatmaps that were found in the current session (useful for ScoreSaber's ranked feeds so beatmaps that get unranked are removed).

__BeatSync.json__
* ~~RegenerateConfig: If true, set all options to defaults on next game start.~~
* DownloadTimeout: How long to wait for a download response before giving up.
* MaxConcurrentDownloads: How many queued beatmaps to download at the same time, higher is not always better - use a reasonable value.
* RecentPlaylistDays: How long recently downloaded beatmaps stay in the BeatSync Recent playlist. (0 to disable the playlist)
* AllBeatSyncSongsPlaylist: If enabled, BeatSync creates a playlist with all the beatmaps it finds (large playlists can cause a big freeze after custom songs are initially loaded)
* BeastSaber:
  * Enabled: If false, disables all feeds from this source.
  * Username: Your BeastSaber username. You must enter this to get beatmaps from the Bookmarks and Follows feeds.
  * MaxConcurrentPageChecks: Number of pages to check simultaneously when reading feeds.
  * Bookmarks: Get bookmarked beatmaps from BeastSaber.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
  * Follows: Get beatmaps from mappers you are following on BeastSaber.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
  * CuratorRecommended: Get beatmaps from BeastSaber's Curator Recommended list.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle
* BeatSaver
  * Enabled: If false, disables all feeds from this source.
  * MaxConcurrentPageChecks: Number of pages to check simultaneously when reading feeds.
  * FavoriteMappers: Feed that downloads beatmaps from each mapper specified in FavoriteMappers.ini (one mapper username per line) in the UserData folder. You can also place FavoriteMappers.ini in BeatSyncConsole's `configs` folder.
    * Enabled
    * MaxSongs: Maximum number of songs to download from each mapper.
    * CreatePlaylist
    * PlaylistStyle
    * SeparateMapperPlaylists: If true (along with CreatePlaylist), create a playlist for each mapper in your FavoriteMappers.ini file.
  * Latest: Get the most recently uploaded beatmaps.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver.**
    * CreatePlaylist
    * PlaylistStyle
* ScoreSaber
  * Enabled: If false, disables all feeds from this source.
  * TopRanked: Get ranked beatmaps by ScoreSaber's "Star" rating.
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked beatmaps get removed.
    * IncludeUnstarred: Include beatmaps that don't have a star rating (no affect on this feed, all beatmaps will have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (beatmaps with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * LatestRanked: Get ScoreSaber's ranked beatmaps, most recently ranked first (more or less).
    * Enabled
    * MaxSongs
    * CreatePlaylist
    * PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked beatmaps get removed.
    * IncludeUnstarred: Include beatmaps that don't have a star rating (no affect on this feed, all beatmaps will have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (beatmaps with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * Trending: Get beatmaps using ScoreSaber's "Trending" ordering.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver if you don't have RankedOnly enabled.**
    * CreatePlaylist
    * PlaylistStyle
    * RankedOnly: If true, only get ranked beatmaps.
    * IncludeUnstarred: Include beatmaps that don't have a star rating (unranked beatmaps can still have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (beatmaps with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.
  * TopPlayed: Get the most played beatmaps as reported by ScoreSaber.
    * Enabled
    * MaxSongs: **WARNING - Setting this to 0 will download every song on BeatSaver if you don't have RankedOnly enabled.**
    * CreatePlaylist
    * PlaylistStyle
    * RankedOnly: If true, only get ranked beatmaps.
    * IncludeUnstarred: Include beatmaps that don't have a star rating (unranked beatmaps can still have a star rating).
    * MaxStars: Maximum star rating for a song, 0 to disable (beatmaps with a lower difficulty will be included if that difficulty is less than this.)
    * MinStars: Maximum star rating for a song, 0 to disable.

# Known Issues
* **Linux:**
  * Some Linux distributions may not work with .Net Core and give the message "No usable version of libssl was found". This may happen for distros, such as Void Linux, that use LibreSSL instead of OpenSSL. .Net Core does not support using LibreSSL, but a possible workaround is to run `ln -s /lib/libssl.so /lib/libssl.so.1.0.0` as root.

# Additional Information
* BeatSync maintains a history file for beatmaps it finds in the feeds. This is used to prevent BeatSync from redownloading beatmaps you've deleted. It is sorted in descending order by date the song was added to the history. This file can safely be deleted (or carefully edited) if you want BeatSync to download beatmaps you've deleted in the past.
* If you use the SongBrowser plugin, it will not show the playlists in the correct order (i.e. TopRanked and LatestRanked will be out of order).

# Credits
* Brian for the original SyncSaber mod.
* JW#1944 (Discord) for the fancy logo.
