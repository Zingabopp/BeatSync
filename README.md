# BeatSync
Beat Saber plugin to automatically download songs.

# Configuration
Configuration is currently only available by editing BeatSync.json in the UserData folder.
* RegenerateConfig: If true, set all options to defaults.
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
  * Follows: Get songs from mappers you are following on BeastSaber.
    * Enabled
	* MaxSongs
	* CreatePlaylist
  * CuratorRecommended: Get songs from BeastSaber's Curator Recommended list.
    * Enabled
	* MaxSongs
	* CreatePlaylist
* BeatSaver
  * Enabled: If false, disables all feeds from this source.
  * MaxConcurrentPageChecks: Number of pages to check simultaneously when reading feeds.
  * FavoriteMappers: Feed that downloads songs from each mapper specified in FavoriteMappers.txt in the UserData folder.
    * Enabled
	* MaxSongs: Maximum number of songs to download from each mapper.
	* CreatePlaylist
	* SeparateMapperPlaylists: Create a playlist for each mapper (Not implemented)
  * Hot: Get songs using Beat Saver's "Hot" ordering.
    * Enabled
	* MaxSongs
	* CreatePlaylist
  * Downloads: Get songs by most downloaded from Beat Saver
    * Enabled
	* MaxSongs
	* CreatePlaylist
* ScoreSaber
  * Enabled: If false, disables all feeds from this source.
  * Trending: Get songs using ScoreSaber's "Trending" ordering.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* RankedOnly: If true, only get ranked songs.
  * TopRanked: Get ranked songs by ScoreSaber's "Star" rating.
    * Enabled
	* MaxSongs
	* CreatePlaylist
  * LatestRanked: Get ScoreSaber's ranked songs, most recently ranked first (more or less).
    * Enabled
	* MaxSongs
	* CreatePlaylist
  * TopPlayed: Get the most played songs as reported by ScoreSaber.
    * Enabled
	* MaxSongs
	* CreatePlaylist
	* RankedOnly: If true, only get ranked songs.
