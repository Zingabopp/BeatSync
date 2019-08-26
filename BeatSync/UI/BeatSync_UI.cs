using BeatSync.Configs;
using CustomUI.Settings;

namespace BeatSync.UI
{
    class BeatSync_UI
    {
        public static PluginConfig Config { get { return Plugin.config.Value; } }
        public static void CreateUI()
        {
            CreateSettingsUI();
            //CreateGameplayOptionsUI();
        }

        /// <summary>
        /// This is the code used to create a submenu in Beat Saber's Settings menu.
        /// </summary>
        public static void CreateSettingsUI()
        {
            ////This will create a menu tab in the settings menu for your plugin
            var pluginSettingsSubmenu = SettingsUI.CreateSubMenu("BeatSync");
            CreateBeatSyncSettingsUI(pluginSettingsSubmenu);
            var beastSaber = pluginSettingsSubmenu.AddSubMenu("BeastSaber", "Settings for the BeastSaber feed source.", true);
            CreateBeastSaberSettingsUI(beastSaber, Config.BeastSaber);
            var beatSaver = pluginSettingsSubmenu.AddSubMenu("Beat Saver", "Settings for the BeatSaver feed source.", true);
            CreateBeatSaverSettingsUI(beatSaver, Config.BeatSaver);
            var scoreSaber = pluginSettingsSubmenu.AddSubMenu("ScoreSaber", "Settings for the ScoreSaber feed source.", true);
            CreateScoreSaberSettingsUI(scoreSaber, Config.ScoreSaber);
        }

        public static void CreateBeatSyncSettingsUI(SubMenu parent)
        {
            var timeoutInt = parent.AddInt("Download Timeout",
                "How long in seconds to wait for Beat Saver to respond to a download request",
                5, 60, 1);
            timeoutInt.GetValue += delegate { return Config.DownloadTimeout; };
            timeoutInt.SetValue += delegate (int value)
            {
                Config.DownloadTimeout = value;
            };

            var maxConcurrentDownloads = parent.AddInt("Max Concurrent Downloads",
                "How many song downloads can happen at the same time.",
                1, 10, 1);
            maxConcurrentDownloads.GetValue += delegate { return Config.MaxConcurrentDownloads; };
            maxConcurrentDownloads.SetValue += delegate (int value)
            {
                Config.MaxConcurrentDownloads = value;
            };

            var recentPlaylistDays = parent.AddInt("Recent Playlist Days",
                "How long in days songs downloaded by BeatSync are kept in the BeatSync Recent playlist (0 to disable the playlist).",
                0, 60, 1);
            recentPlaylistDays.GetValue += delegate { return Config.RecentPlaylistDays; };
            recentPlaylistDays.SetValue += delegate (int value)
            {
                Config.RecentPlaylistDays = value;
            };

            var allBeatSyncSongs = parent.AddBool("Enable All BeatSync Songs",
                "Maintain a playlist that has all the songs downloaded by BeatSync.");
            allBeatSyncSongs.GetValue += delegate { return Config.AllBeatSyncSongsPlaylist; };
            allBeatSyncSongs.SetValue += delegate (bool value)
            {
                Config.AllBeatSyncSongsPlaylist = value;
            };
        }

        public static void CreateBeastSaberSettingsUI(SubMenu parent, BeastSaberConfig sourceConfig)
        {
            string sourceName = "BeastSaber";

            CreateSourceSettings(sourceName, parent, sourceConfig);
            var maxConcurrentPageChecks = parent.AddInt("Max Concurrent Page Checks",
                $"How many {sourceName} pages to check at the same time.",
                1, 15, 1);
            maxConcurrentPageChecks.GetValue += delegate { return sourceConfig.MaxConcurrentPageChecks; };
            maxConcurrentPageChecks.SetValue += delegate (int value)
            {
                sourceConfig.MaxConcurrentPageChecks = value;
            };

            var username = parent.AddString("Username", "Your BeastSaber username. Required to use the Bookmarks and Follows feeds");
            username.GetValue += delegate { return sourceConfig.Username ?? ""; };
            username.SetValue += delegate (string value)
            {
                sourceConfig.Username = value;
            };

            var bookmarks = CreateFeedSettings("Bookmarks", sourceName, sourceConfig.Bookmarks, parent);
            var follows = CreateFeedSettings("Follows", sourceName, sourceConfig.Follows, parent);
            var curator = CreateFeedSettings("Curator Recommended", sourceName, sourceConfig.CuratorRecommended, parent);
        }

        public static void CreateBeatSaverSettingsUI(SubMenu parent, BeatSaverConfig sourceConfig)
        {
            string sourceName = "BeatSaver";

            CreateSourceSettings(sourceName, parent, sourceConfig);
            var maxConcurrentPageChecks = parent.AddInt("Max Concurrent Page Checks",
                $"How many {sourceName} pages to check at the same time.",
                1, 15, 1);
            maxConcurrentPageChecks.GetValue += delegate { return sourceConfig.MaxConcurrentPageChecks; };
            maxConcurrentPageChecks.SetValue += delegate (int value)
            {
                sourceConfig.MaxConcurrentPageChecks = value;
            };

            var favoriteMappers = CreateFeedSettings("Favorite Mappers", sourceName, sourceConfig.FavoriteMappers, parent, "Feed to get songs from mappers listed in UserDate\\FavoriteMappers.ini.");
            var hot = CreateFeedSettings("Hot", sourceName, sourceConfig.Hot, parent);
            var downloads = CreateFeedSettings("Downloads", sourceName, sourceConfig.Downloads, parent);
        }

        public static void CreateScoreSaberSettingsUI(SubMenu parent, ScoreSaberConfig sourceConfig)
        {
            string sourceName = "ScoreSaber";
            CreateSourceSettings(sourceName, parent, sourceConfig);

            var latestRanked = CreateFeedSettings("Latest Ranked", sourceName, sourceConfig.LatestRanked, parent);
            var topRanked = CreateFeedSettings("Top Ranked", sourceName, sourceConfig.TopRanked, parent);
            var trending = CreateFeedSettings("Trending", sourceName, sourceConfig.Trending, parent);
            var trendingRankedOnly = trending.AddBool("Ranked Only",
                            $"Get only ranked songs from the Trending feed.");
            trendingRankedOnly.GetValue += delegate { return sourceConfig.Trending.RankedOnly; };
            trendingRankedOnly.SetValue += delegate (bool value)
            {
                sourceConfig.Trending.RankedOnly = value;
            };
            var topPlayed = CreateFeedSettings("Top Played", sourceName, sourceConfig.TopPlayed, parent);
            var topPlayedRankedOnly = topPlayed.AddBool("Ranked Only",
                            $"Get only ranked songs from the Trending feed.");
            topPlayedRankedOnly.GetValue += delegate { return sourceConfig.TopPlayed.RankedOnly; };
            topPlayedRankedOnly.SetValue += delegate (bool value)
            {
                sourceConfig.TopPlayed.RankedOnly = value;
            };
        }

        public static void CreateSourceSettings(string sourceName, SubMenu parent, SourceConfigBase sourceConfig)
        {
            var enabled = parent.AddBool("Enable",
                            $"Read enabled feeds from {sourceName}.");
            enabled.GetValue += delegate { return sourceConfig.Enabled; };
            enabled.SetValue += delegate (bool value)
            {
                sourceConfig.Enabled = value;
            };

        }

        public static SubMenu CreateFeedSettings(string feedName, string feedSource, FeedConfigBase feedConfig, SubMenu parent, string menuHintText = "")
        {
            if (string.IsNullOrEmpty(menuHintText))
                menuHintText = $"Settings for {feedSource}'s {feedName} feed";
            var feedSubMenu = parent.AddSubMenu(feedName, menuHintText, true);
            var enabled = feedSubMenu.AddBool("Enable",
                            $"Enable the {feedName} feed from {feedSource}.");
            enabled.GetValue += delegate { return feedConfig.Enabled; };
            enabled.SetValue += delegate (bool value)
            {
                feedConfig.Enabled = value;
            };

            var maxSongs = feedSubMenu.AddInt("Max Songs",
                "Maximum number of songs to download (0 for all).",
                0, 500, 5);
            maxSongs.GetValue += delegate { return feedConfig.MaxSongs; };
            maxSongs.SetValue += delegate (int value)
            {
                feedConfig.MaxSongs = value;
            };

            var createPlaylist = feedSubMenu.AddBool("Create Playlist",
                            $"Maintain a playlist for this feed.");
            createPlaylist.GetValue += delegate { return feedConfig.CreatePlaylist; };
            createPlaylist.SetValue += delegate (bool value)
            {
                feedConfig.CreatePlaylist = value;
            };
            return feedSubMenu;
        }

        /// <summary>
        /// This is the code used to create a submenu in Beat Saber's Gameplay Settings panel.
        /// </summary>
        public static void CreateGameplayOptionsUI()
        {
            //string pluginName = "BeatSync";
            //string parentMenu = "MainMenu";
            //string subMenuName = "ExamplePluginMenu"; // Name of SubMenu, pass this to the settings you want to have in this menu.
            ////Example submenu option
            //var pluginSubmenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.ModifiersLeft, pluginName, parentMenu, subMenuName, "You can keep all your plugin's gameplay options nested within this one button");

            ////Example Toggle Option within a submenu
            //var exampleToggle = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Example Toggle", subMenuName, "Put a toggle for a setting you want easily accessible in game here.");
            //exampleToggle.GetValue = /* Fetch the initial value for the option here*/ false;
            //exampleToggle.OnToggle += (value) =>
            //{
            //    /*  You can execute whatever you want to occur when the value is toggled here, usually that would include updating wherever the value is pulled from   */
            //    Logger.log?.Debug($"Toggle is {(value ? "On" : "Off")}");
            //};
            //exampleToggle.AddConflict("Conflicting Option Name"); //You can add conflicts with other gameplay options settings here, preventing both from being active at the same time, including that of other mods

            //// Creates a list setting you can scroll through with backwards and forwards buttons.
            //float[] floatValues = { 0f, 1f, 2f, 3f, 4f };
            //string[] textValues = { "ex0", "ex1", "ex2", "ex3", "ex4" };
            //var exampleList = GameplaySettingsUI.CreateListOption(GameplaySettingsPanels.ModifiersLeft, "Example List", subMenuName, "List example mouse over description");
            //exampleList.GetValue += delegate { return Plugin.ExampleGameplayListSetting; };
            //for (int i = 0; i < 5; i++)
            //    exampleList.AddOption(i, textValues[i]);
            //exampleList.OnChange += (value) =>
            //{
            //    // Execute code based on what value is selected.
            //    Plugin.ExampleGameplayListSetting = value;
            //    Logger.log?.Debug($"Example GameplaySetting List value changed to {textValues[(int)value]}");
            //};
        }

    }


}

namespace BeatSync
{
    /// <summary>
    /// Adds .ToFloatAry() method to instances of the UnityEngine.Color class, and .ToColor() to float arrays.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Converts a color to a float array. Ex: float[] colorAry = color.ToFloat()
        /// </summary>
        /// <param name="color">Color to convert to a float array</param>
        /// <returns>Float array with the color values</returns>
        public static float[] ToFloatAry(this UnityEngine.Color color)
        {
            return new float[] { color.r, color.g, color.b, color.a };
        }

        /// <summary>
        /// Converts a float array to a color. Ex: Color color = floatAry.ToColor()
        /// </summary>
        /// <param name="rgbaVals">Float[4] array with the values [Red, Green, Blue, Alpha] </param>
        /// <returns>A UnityEngine.Color</returns>
        public static UnityEngine.Color ToColor(this float[] rgbaVals)
        {
            return new UnityEngine.Color(rgbaVals[0], rgbaVals[1], rgbaVals[2], rgbaVals[3]);
        }
    }
}