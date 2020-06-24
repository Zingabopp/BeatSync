//using BeatSync.Configs;
//using System;

//namespace BeatSync.UI
//{
//    internal class BeatSync_UI
//    {
//        struct ListParameters
//        {
//            public ListParameters(string name, float min, float max, float increment)
//            {
//                Name = name;
//                Minimum = min;
//                Maximum = max;
//                Increment = increment;
//            }
//            public string Name;
//            public float Minimum;
//            public float Maximum;
//            public float Increment;
//            public override string ToString()
//            {
//                return $"{Name}: {Minimum}, {Maximum}, {Increment}";
//            }
//        }
//        private static ListParameters RowSpacingParameters = new ListParameters("RowSpacingParameters", 0.05f, 5.0f, 0.05f);
//        private static ListParameters DistanceParameters = new ListParameters("DistanceParameters", 0.1f, 10.0f, 0.1f);
//        private static ListParameters HeightParameters = new ListParameters("HeightParameters", -5f, 5.0f, 0.2f);

//        public static float[] RowSpacingList = GetFloatList(RowSpacingParameters);
//        public static float[] DistanceList = GetFloatList(DistanceParameters);
//        public static float[] HeightList = GetFloatList(HeightParameters);
//        public static PluginConfig Config { get { return Plugin.config.Value; } }
//        public static void CreateUI()
//        {
//            CreateSettingsUI();
//            //CreateGameplayOptionsUI();
//        }

//        /// <summary>
//        /// This is the code used to create a submenu in Beat Saber's Settings menu.
//        /// </summary>
//        public static void CreateSettingsUI()
//        {
//            Plugin.log?.Debug($"Creating settings UI.");
//            try
//            {
//                ////This will create a menu tab in the settings menu for your plugin
//                var pluginSettingsSubmenu = SettingsUI.CreateSubMenu("BeatSync", true);
//                CreateBeatSyncSettingsUI(pluginSettingsSubmenu);
//                var sourceSettings = pluginSettingsSubmenu.AddSubMenu("Source Settings", "Settings to configure song sources.", true);
//                CreateSourceSettingsUI(sourceSettings);
//                var statusUiSettings = pluginSettingsSubmenu.AddSubMenu("Status UI Settings", "Settings to configure the in-game status display.", true);
//                CreateStatusUiSettings(statusUiSettings);
//            }
//            catch (Exception ex)
//            {
//                Plugin.log?.Error($"Error creating settings UI: {ex.Message}");
//                Plugin.log?.Debug(ex);
//            }

//        }

//        public static void CreateBeatSyncSettingsUI(SubMenu parent)
//        {
//            var timeoutInt = parent.AddInt("Download Timeout",
//                "How long in seconds to wait for Beat Saver to respond to a download request",
//                5, 60, 1);
//            timeoutInt.GetValue += delegate { return Config.DownloadTimeout; };
//            timeoutInt.SetValue += delegate (int value)
//            {
//                if (Config.DownloadTimeout == value)
//                    return;
//                Config.DownloadTimeout = value;
//                //Config.SetConfigChanged();
//            };

//            var maxConcurrentDownloads = parent.AddInt("Max Concurrent Downloads",
//                "How many song downloads can happen at the same time.",
//                1, 10, 1);
//            maxConcurrentDownloads.GetValue += delegate { return Config.MaxConcurrentDownloads; };
//            maxConcurrentDownloads.SetValue += delegate (int value)
//            {
//                if (Config.MaxConcurrentDownloads == value)
//                    return;
//                Config.MaxConcurrentDownloads = value;
//                // Config.ConfigChanged = true;
//            };

//            var recentPlaylistDays = parent.AddInt("Recent Playlist Days",
//                "How long in days songs downloaded by BeatSync are kept in the BeatSync Recent playlist (0 to disable the playlist).",
//                0, 60, 1);
//            recentPlaylistDays.GetValue += delegate { return Config.RecentPlaylistDays; };
//            recentPlaylistDays.SetValue += delegate (int value)
//            {
//                if (Config.RecentPlaylistDays == value)
//                    return;
//                Config.RecentPlaylistDays = value;
//                // Config.ConfigChanged = true;
//            };

//            var allBeatSyncSongs = parent.AddBool("Enable All BeatSync Songs",
//                "Maintain a playlist that has all the songs downloaded by BeatSync.");
//            allBeatSyncSongs.GetValue += delegate { return Config.AllBeatSyncSongsPlaylist; };
//            allBeatSyncSongs.SetValue += delegate (bool value)
//            {
//                if (Config.AllBeatSyncSongsPlaylist == value)
//                    return;
//                Config.AllBeatSyncSongsPlaylist = value;
//                // Config.ConfigChanged = true;
//            };

//            var timeBetweenSyncs = parent.AddSubMenu("Time Between Syncs",
//                "Minimum amount of time between BeatSync syncs (Set both to 0 to run BeatSync every time the game is started).", true);
//            var hours = timeBetweenSyncs.AddInt("Hours", "Number of hours between BeatSync syncs.", 0, 100, 1);
//            hours.GetValue += delegate { return Config.TimeBetweenSyncs.Hours; };
//            hours.SetValue += delegate (int value)
//            {
//                if (Config.TimeBetweenSyncs.Hours == value)
//                    return;
//                Config.TimeBetweenSyncs.Hours = value;
//                // Config.ConfigChanged = true;
//            };

//            var minutes = timeBetweenSyncs.AddInt("Minutes", "Number of minutes between BeatSync syncs.", 0, 59, 1);
//            minutes.GetValue += delegate { return Config.TimeBetweenSyncs.Minutes; };
//            minutes.SetValue += delegate (int value)
//            {
//                if (Config.TimeBetweenSyncs.Minutes == value)
//                    return;
//                Config.TimeBetweenSyncs.Minutes = value;
//                // Config.ConfigChanged = true;
//            };
//        }

//        public static void CreateSourceSettingsUI(SubMenu parent)
//        {
//            var beastSaber = parent.AddSubMenu("BeastSaber", "Settings for the BeastSaber feed source.", true);
//            CreateBeastSaberSettingsUI(beastSaber, Config.BeastSaber);
//            var beatSaver = parent.AddSubMenu("Beat Saver", "Settings for the BeatSaver feed source.", true);
//            CreateBeatSaverSettingsUI(beatSaver, Config.BeatSaver);
//            var scoreSaber = parent.AddSubMenu("ScoreSaber", "Settings for the ScoreSaber feed source.", true);
//            CreateScoreSaberSettingsUI(scoreSaber, Config.ScoreSaber);
//        }

//        public static void CreateStatusUiSettings(SubMenu parent)
//        {
//            var textRowsInt = parent.AddInt("Text Rows",
//                "Number of text rows to show for each source (not including Header).",
//                1, 20, 1);
//            textRowsInt.GetValue += delegate { return Config.StatusUI.TextRows; };
//            textRowsInt.SetValue += delegate (int value)
//            {
//                if (Config.StatusUI.TextRows == value)
//                    return;
//                Config.StatusUI.TextRows = value;
//                Plugin.StatusController?.UpdateSettings();
//            };

//            var fadeTimeInt = parent.AddInt("Fade Time",
//                "Time in seconds the Status UI will be displayed after BeatSync finishes (0 to never fade).",
//                0, 60, 1);
//            fadeTimeInt.GetValue += delegate { return Config.StatusUI.FadeTime; };
//            fadeTimeInt.SetValue += delegate (int value)
//            {
//                if (Config.StatusUI.FadeTime == value)
//                    return;
//                Config.StatusUI.FadeTime = value;
//                Plugin.StatusController?.UpdateSettings();
//            };

//            var rowSpacingFloat = parent.AddList("Row Spacing", RowSpacingList,
//                "Amount of space between text rows.");

//            rowSpacingFloat.GetValue += delegate { return Config.StatusUI.RowSpacing; };
//            rowSpacingFloat.SetValue += delegate (float value)
//            {
//                if (Config.StatusUI.RowSpacing == value)
//                    return;
//                Config.StatusUI.RowSpacing = value;
//                Plugin.StatusController?.UpdateSettings();
//            };
//            rowSpacingFloat.GetTextForValue = (val) => val.ToString("0.00");
//            rowSpacingFloat.applyImmediately = true;

//            var distanceFloat = parent.AddList("Distance", DistanceList,
//                "Distance from the center of the platform.");
//            distanceFloat.GetValue += delegate { return Config.StatusUI.Distance; };
//            distanceFloat.SetValue += delegate (float value)
//            {
//                if (Config.StatusUI.Distance == value)
//                    return;
//                Config.StatusUI.Distance = value;
//                Plugin.StatusController?.UpdateSettings();
//            };
//            distanceFloat.GetTextForValue = (val) => val.ToString("0.00");
//            distanceFloat.applyImmediately = true;

//            var heightFloat = parent.AddList("Height", HeightList,
//                "Height from the bottom of the platform.");
//            heightFloat.GetValue += delegate { return Config.StatusUI.Height; };
//            heightFloat.SetValue += delegate (float value)
//            {
//                if (Config.StatusUI.Height == value)
//                    return;
//                Config.StatusUI.Height = value;
//                Plugin.StatusController?.UpdateSettings();
//            };
//            heightFloat.GetTextForValue = (val) => val.ToString("0.00");
//            heightFloat.applyImmediately = true;

//            var horizontalAngleInt = parent.AddInt("Horizontal Angle",
//                "Horizontal rotation of the Status UI.",
//                -360, 360, 2);
//            horizontalAngleInt.GetValue += delegate { return Config.StatusUI.HorizontalAngle; };
//            horizontalAngleInt.SetValue += delegate (int value)
//            {
//                if (Config.StatusUI.HorizontalAngle == value)
//                    return;
//                Config.StatusUI.HorizontalAngle = value;
//                Plugin.StatusController?.UpdateSettings();
//            };
//            horizontalAngleInt.applyImmediately = true;
//        }

//        private static float[] GetFloatList(ListParameters p)
//        {
//            //Plugin.log?.Warn($"FloatList: {p}");
//            return GetFloatList(p.Minimum, p.Maximum, p.Increment);
//        }
//        private static float[] GetFloatList(float min, float max, float increment)
//        {

//            int numValues = (int)((max - min) / increment);
//            var list = new float[numValues];
//            float curVal = min;
//            for (int i = 0; i < numValues; i++)
//            {
//                list[i] = (float)Math.Round(curVal, 2);
//                curVal = curVal + increment;
//            }
//            return list;
//        }

//        public static void CreateBeastSaberSettingsUI(SubMenu parent, BeastSaberConfig sourceConfig)
//        {
//            string sourceName = "BeastSaber";

//            CreateSourceSettings(sourceName, parent, sourceConfig);
//            var maxConcurrentPageChecks = parent.AddInt("Max Concurrent Page Checks",
//                $"How many {sourceName} pages to check at the same time.",
//                1, 15, 1);
//            maxConcurrentPageChecks.GetValue += delegate { return sourceConfig.MaxConcurrentPageChecks; };
//            maxConcurrentPageChecks.SetValue += delegate (int value)
//            {
//                if (sourceConfig.MaxConcurrentPageChecks == value)
//                    return;
//                sourceConfig.MaxConcurrentPageChecks = value;
//                // Config.ConfigChanged = true;
//            };

//            var username = parent.AddString("Username", "Your BeastSaber username. Required to use the Bookmarks and Follows feeds");
//            username.GetValue += delegate { return sourceConfig.Username ?? ""; };
//            username.SetValue += delegate (string value)
//            {
//                if (sourceConfig.Username == value)
//                    return;
//                sourceConfig.Username = value;
//                // Config.ConfigChanged = true;
//            };

//            var bookmarks = CreateFeedSettings("Bookmarks", sourceName, sourceConfig.Bookmarks, parent);
//            var follows = CreateFeedSettings("Follows", sourceName, sourceConfig.Follows, parent);
//            var curator = CreateFeedSettings("Curator Recommended", sourceName, sourceConfig.CuratorRecommended, parent);
//        }

//        public static void CreateBeatSaverSettingsUI(SubMenu parent, BeatSaverConfig sourceConfig)
//        {
//            string sourceName = "BeatSaver";

//            CreateSourceSettings(sourceName, parent, sourceConfig);
//            var maxConcurrentPageChecks = parent.AddInt("Max Concurrent Page Checks",
//                $"How many {sourceName} pages to check at the same time.",
//                1, 15, 1);
//            maxConcurrentPageChecks.GetValue += delegate { return sourceConfig.MaxConcurrentPageChecks; };
//            maxConcurrentPageChecks.SetValue += delegate (int value)
//            {
//                if (sourceConfig.MaxConcurrentPageChecks == value)
//                    return;
//                sourceConfig.MaxConcurrentPageChecks = value;
//                // Config.ConfigChanged = true;
//            };

//            var favoriteMappers = CreateFeedSettings("Favorite Mappers", sourceName, sourceConfig.FavoriteMappers, parent, "Feed to get songs from mappers listed in UserData\\FavoriteMappers.ini. Max Songs is per mapper.");
//            var separateMapperPlaylists = favoriteMappers.AddBool("Separate Mapper Playlists",
//                            $"Create a playlist for each mapper in FavoriteMappers.ini (must also have CreatePlaylist enabled).");
//            separateMapperPlaylists.GetValue += delegate { return sourceConfig.FavoriteMappers.SeparateMapperPlaylists; };
//            separateMapperPlaylists.SetValue += delegate (bool value)
//            {
//                if (sourceConfig.FavoriteMappers.SeparateMapperPlaylists == value)
//                    return;
//                sourceConfig.FavoriteMappers.SeparateMapperPlaylists = value;
//            };
//            var hot = CreateFeedSettings("Hot", sourceName, sourceConfig.Hot, parent);
//            var downloads = CreateFeedSettings("Downloads", sourceName, sourceConfig.Downloads, parent);
//        }

//        public static void CreateScoreSaberSettingsUI(SubMenu parent, ScoreSaberConfig sourceConfig)
//        {
//            string sourceName = "ScoreSaber";
//            CreateSourceSettings(sourceName, parent, sourceConfig);

//            var latestRanked = CreateFeedSettings("Latest Ranked", sourceName, sourceConfig.LatestRanked, parent);
//            var topRanked = CreateFeedSettings("Top Ranked", sourceName, sourceConfig.TopRanked, parent);
//            var trending = CreateFeedSettings("Trending", sourceName, sourceConfig.Trending, parent);
//            var trendingRankedOnly = trending.AddBool("Ranked Only",
//                            $"Get only ranked songs from the Trending feed.");
//            trendingRankedOnly.GetValue += delegate { return sourceConfig.Trending.RankedOnly; };
//            trendingRankedOnly.SetValue += delegate (bool value)
//            {
//                if (sourceConfig.Trending.RankedOnly == value)
//                    return;
//                sourceConfig.Trending.RankedOnly = value;
//                // Config.ConfigChanged = true;
//            };
//            var topPlayed = CreateFeedSettings("Top Played", sourceName, sourceConfig.TopPlayed, parent);
//            var topPlayedRankedOnly = topPlayed.AddBool("Ranked Only",
//                            $"Get only ranked songs from the Trending feed.");
//            topPlayedRankedOnly.GetValue += delegate { return sourceConfig.TopPlayed.RankedOnly; };
//            topPlayedRankedOnly.SetValue += delegate (bool value)
//            {
//                if (sourceConfig.TopPlayed.RankedOnly == value)
//                    return;
//                sourceConfig.TopPlayed.RankedOnly = value;
//                // Config.ConfigChanged = true;
//            };
//        }

//        public static void CreateSourceSettings(string sourceName, SubMenu parent, SourceConfigBase sourceConfig)
//        {
//            var enabled = parent.AddBool("Enable",
//                            $"Read enabled feeds from {sourceName}.");
//            enabled.GetValue += delegate { return sourceConfig.Enabled; };
//            enabled.SetValue += delegate (bool value)
//            {
//                if (sourceConfig.Enabled == value)
//                    return;
//                sourceConfig.Enabled = value;
//                // Config.ConfigChanged = true;
//            };

//        }

//        public static SubMenu CreateFeedSettings(string feedName, string feedSource, FeedConfigBase feedConfig, SubMenu parent, string menuHintText = "")
//        {
//            if (string.IsNullOrEmpty(menuHintText))
//                menuHintText = $"Settings for {feedSource}'s {feedName} feed";
//            var feedSubMenu = parent.AddSubMenu(feedName, menuHintText, true);
//            var enabled = feedSubMenu.AddBool("Enable",
//                            $"Enable the {feedName} feed from {feedSource}.");
//            enabled.GetValue += delegate { return feedConfig.Enabled; };
//            enabled.SetValue += delegate (bool value)
//            {
//                if (feedConfig.Enabled == value)
//                    return;
//                feedConfig.Enabled = value;
//                // Config.ConfigChanged = true;
//            };

//            var maxSongs = feedSubMenu.AddInt("Max Songs",
//                "Maximum number of songs to download (0 for all).",
//                0, 500, 5);
//            maxSongs.GetValue += delegate { return feedConfig.MaxSongs; };
//            maxSongs.SetValue += delegate (int value)
//            {
//                if (feedConfig.MaxSongs == value)
//                    return;
//                feedConfig.MaxSongs = value;
//                // Config.ConfigChanged = true;
//            };

//            var createPlaylist = feedSubMenu.AddBool("Create Playlist",
//                            $"Maintain a playlist for this feed.");
//            createPlaylist.GetValue += delegate { return feedConfig.CreatePlaylist; };
//            createPlaylist.SetValue += delegate (bool value)
//            {
//                if (feedConfig.CreatePlaylist == value)
//                    return;
//                feedConfig.CreatePlaylist = value;
//                // Config.ConfigChanged = true;
//            };

//            string[] textSegmentOptions = new string[] { "Append", "Replace" };
//            var textSegmentsExample = feedSubMenu.AddTextSegments("Playlist Style", "Select 'Append' to add new songs to playlist, 'Replace' to create a fresh playlist with songs read from the feed this session.", textSegmentOptions);
//            textSegmentsExample.GetValue += delegate { return feedConfig.PlaylistStyle == PlaylistStyle.Append ? 0 : 1; };
//            textSegmentsExample.SetValue += delegate (int value)
//            {
//                PlaylistStyle newStyle = value == 0 ? PlaylistStyle.Append : PlaylistStyle.Replace;
//                if (feedConfig.PlaylistStyle == newStyle)
//                    return;
//                feedConfig.PlaylistStyle = newStyle;
//                // Config.ConfigChanged = true;
//            };
//            return feedSubMenu;
//        }

//        /// <summary>
//        /// This is the code used to create a submenu in Beat Saber's Gameplay Settings panel.
//        /// </summary>
//        public static void CreateGameplayOptionsUI()
//        {
//            //string pluginName = "BeatSync";
//            //string parentMenu = "MainMenu";
//            //string subMenuName = "ExamplePluginMenu"; // Name of SubMenu, pass this to the settings you want to have in this menu.
//            ////Example submenu option
//            //var pluginSubmenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.ModifiersLeft, pluginName, parentMenu, subMenuName, "You can keep all your plugin's gameplay options nested within this one button");

//            ////Example Toggle Option within a submenu
//            //var exampleToggle = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Example Toggle", subMenuName, "Put a toggle for a setting you want easily accessible in game here.");
//            //exampleToggle.GetValue = /* Fetch the initial value for the option here*/ false;
//            //exampleToggle.OnToggle += (value) =>
//            //{
//            //    /*  You can execute whatever you want to occur when the value is toggled here, usually that would include updating wherever the value is pulled from   */
//            //    Plugin.log?.Debug($"Toggle is {(value ? "On" : "Off")}");
//            //};
//            //exampleToggle.AddConflict("Conflicting Option Name"); //You can add conflicts with other gameplay options settings here, preventing both from being active at the same time, including that of other mods

//            //// Creates a list setting you can scroll through with backwards and forwards buttons.
//            //float[] floatValues = { 0f, 1f, 2f, 3f, 4f };
//            //string[] textValues = { "ex0", "ex1", "ex2", "ex3", "ex4" };
//            //var exampleList = GameplaySettingsUI.CreateListOption(GameplaySettingsPanels.ModifiersLeft, "Example List", subMenuName, "List example mouse over description");
//            //exampleList.GetValue += delegate { return Plugin.ExampleGameplayListSetting; };
//            //for (int i = 0; i < 5; i++)
//            //    exampleList.AddOption(i, textValues[i]);
//            //exampleList.OnChange += (value) =>
//            //{
//            //    // Execute code based on what value is selected.
//            //    Plugin.ExampleGameplayListSetting = value;
//            //    Plugin.log?.Debug($"Example GameplaySetting List value changed to {textValues[(int)value]}");
//            //};
//        }

//    }


//}