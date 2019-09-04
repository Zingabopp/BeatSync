# BeatSync
Beat Saber plugin to automatically download songs.
The following sources and feeds are currently supported:
 * Beast Saber: Bookmarks, Follows, Curator Recommended
 * Beat Saver: FavoriteMappers, Hot, Downloads
 * ScoreSaber: Trending, Top Ranked, Latest Ranked, Top Played
# Installation
* Extract the release zip to your Beat Saber folder (or manually place BeatSync.dll into your Plugins folder).
* You must also have the following mods for BeatSync to function:
  * BSIPA 
  * SongCore
  * CustomUI
  * BeatSaverDownloader

# Usage
**First Run:**
* Run Beat Saber after installing the mod and configure the settings in-game (Settings > BeatSync) or in UserData\BeatSync.json.
* Beat Saber must be restarted for it use the modified settings to download songs.

# Configuration
Configuration can be done in-game in Settings > BeatSync or by (carefully) editing the BeatSync.json file in your UserData folder.

__Each feed has the following options:__
* Enabled: If true, BeatSync will use this feed to download songs.
* MaxSongs: Maximum number of songs to download from that feed.
* CreatePlaylist: Creates/Maintains a playlist using songs from the feed. Songs that are found by the feed but have already been downloaded are also added to the playlist.
* PlaylistStyle: If set to 'Append' (0 in the json), each session will add the songs it finds to the playlist. If set to 'Replace (1 in the json), the playlist will only contain songs that were found in the current session (useful for ScoreSaber's ranked feeds so songs that get unranked are removed).

__BeatSync.json__
* RegenerateConfig: If true, set all options to defaults on next game start.
* DownloadTimeout: How long to wait for a download response before giving up.
* MaxConcurrentDownloads: How many queued songs to download at the same time.
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
  * FavoriteMappers: Feed that downloads songs from each mapper specified in FavoriteMappers.txt in the UserData folder.
    * Enabled
	* MaxSongs: Maximum number of songs to download from each mapper.
	* CreatePlaylist
	* PlaylistStyle
  * Hot: Get songs using Beat Saver's "Hot" ordering.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle
	* SeparateMapperPlaylists: Creates a playlist for each mapper specified in FavoriteMappers.ini (Must also have CreatePlaylist enabled).
  * Downloads: Get songs by most downloaded from Beat Saver
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle
* ScoreSaber
  * Enabled: If false, disables all feeds from this source.
  * TopRanked: Get ranked songs by ScoreSaber's "Star" rating.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked songs gets removed.
  * LatestRanked: Get ScoreSaber's ranked songs, most recently ranked first (more or less).
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle: Recommended to leave this set to 'Replace' (1 in json) so that unranked songs gets removed.
  * Trending: Get songs using ScoreSaber's "Trending" ordering.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle
	* RankedOnly: If true, only get ranked songs.
  * TopPlayed: Get the most played songs as reported by ScoreSaber.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* PlaylistStyle
	* RankedOnly: If true, only get ranked songs.
	
# Additional Information
* BeatSync maintains a history file for songs it finds in the feeds. This is used to prevent BeatSync from redownloading songs you've deleted. It is sorted in descending order by date the song was added to the history. This file can safely be deleted (or carefully edited) if you want BeatSync to download songs you've deleted in the past.
