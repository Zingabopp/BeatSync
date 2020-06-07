//using BeatSyncConsole.Configs;
//using BeatSyncConsole.Loggers;
//using BeatSyncLib.Configs;
//using BeatSyncLib.Downloader;
//using BeatSyncLib.Downloader.Downloading;
//using BeatSyncLib.Downloader.Targets;
//using BeatSyncPlaylists;
//using SongFeedReaders.Data;
//using SongFeedReaders.Readers;
//using SongFeedReaders.Readers.BeastSaber;
//using SongFeedReaders.Readers.BeatSaver;
//using SongFeedReaders.Readers.ScoreSaber;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BeatSyncConsole
//{
//    public class SongDownloader
//    {
//        public event EventHandler<string>? SourceStarted;
//        public async Task<JobStats[]> RunAsync(Config config, IJobBuilder jobBuilder, JobManager manager)
//        {
//            Task<JobStats>[] downloadTasks = new Task<JobStats>[]
//                    {
//                        GetBeatSaverAsync(config.BeatSyncConfig, jobBuilder, manager),
//                        GetBeastSaberAsync(config.BeatSyncConfig, jobBuilder, manager),
//                        GetScoreSaberAsync(config.BeatSyncConfig, jobBuilder, manager)
//                    };
//            return await Task.WhenAll(downloadTasks).ConfigureAwait(false);
//        }

//        public static IEnumerable<IJob>? CreateJobs(FeedResult feedResult, IJobBuilder jobBuilder, JobManager jobManager)
//        {
//            if (!feedResult.Successful)
//                return null;
//            if (feedResult.Songs.Count == 0)
//            {
//                Logger.log.Info("No songs");
//                return Array.Empty<IJob>();
//            }
//            List<IJob> jobs = new List<IJob>(feedResult.Count);

//            foreach (ScrapedSong song in feedResult.Songs.Values)
//            {
//                Job newJob = jobBuilder.CreateJob(song);
//                jobManager.TryPostJob(newJob, out IJob? postedJob);
//                if (postedJob != null)
//                    jobs.Add(postedJob);
//                else
//                    Logger.log.Info($"Posted job is null for {song}, this shouldn't happen.");
//            }
//            return jobs;
//        }

//        static async Task<JobStats> GetScoreSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
//        {
//            ScoreSaberConfig sourceConfig = config.ScoreSaber;
//            JobStats sourceStats = new JobStats();
//            if (!sourceConfig.Enabled)
//                return sourceStats;
//            ScoreSaberReader reader = new ScoreSaberReader();
//            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.TopRanked, sourceConfig.LatestRanked, sourceConfig.Trending, sourceConfig.TopPlayed };
//            if (!feedConfigs.Any(f => f.Enabled))
//            {
//                Logger.log.Info($"No feeds enabled for {reader.Name}");
//                return sourceStats;
//            }
//            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
//            {
//                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
//                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
//                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
//                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
//                JobStats feedStats = new JobStats(jobResults);
//                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
//                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
//                sourceStats += feedStats;
//            }

//            Logger.log.Info($"  Finished ScoreSaber reading: ({sourceStats}).");
//            return sourceStats;
//        }

//        static async Task<JobStats> GetBeastSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
//        {
//            BeastSaberConfig sourceConfig = config.BeastSaber;
//            JobStats sourceStats = new JobStats();
//            if (!sourceConfig.Enabled)
//                return sourceStats;
//            BeastSaberReader reader = new BeastSaberReader(sourceConfig.Username, sourceConfig.MaxConcurrentPageChecks);
//            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Bookmarks, sourceConfig.Follows, sourceConfig.CuratorRecommended };
//            if (!feedConfigs.Any(f => f.Enabled))
//            {
//                Logger.log.Info($"No feeds enabled for {reader.Name}");
//                return sourceStats;
//            }
//            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
//            {
//                if (string.IsNullOrEmpty(sourceConfig.Username) && feedConfig.GetType() != typeof(BeastSaberCuratorRecommended))
//                {
//                    Logger.log.Warn($"  {feedConfig.GetType().Name} feed not available without a valid username.");
//                    continue;
//                }
//                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
//                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
//                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
//                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
//                JobStats feedStats = new JobStats(jobResults);
//                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
//                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
//                sourceStats += feedStats;
//            }
//            Logger.log.Info($"  Finished BeastSaber reading: ({sourceStats}).");
//            return sourceStats;
//        }

//        static async Task<JobStats> GetBeatSaverAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
//        {
//            BeatSaverConfig sourceConfig = config.BeatSaver;
//            JobStats sourceStats = new JobStats();
//            if (!sourceConfig.Enabled)
//                return sourceStats;

//            BeatSaverReader reader = new BeatSaverReader(sourceConfig.MaxConcurrentPageChecks);
//            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Hot, sourceConfig.Downloads };
//            if (!(feedConfigs.Any(f => f.Enabled) || sourceConfig.FavoriteMappers.Enabled))
//            {
//                Logger.log.Info($"No feeds enabled for {reader.Name}");
//                return sourceStats;
//            }
//            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
//            {
//                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
//                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
//                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
//                await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
//                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
//                JobStats feedStats = new JobStats(jobResults);
//                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
//                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
//                sourceStats += feedStats;
//            }

//            string[] mappers = sourceConfig.FavoriteMappers.Mappers ?? Array.Empty<string>();
//            if (sourceConfig.FavoriteMappers.Enabled)
//            {
//                FeedConfigBase feedConfig = sourceConfig.FavoriteMappers;
//                if (mappers.Length > 0)
//                {
//                    Logger.log.Info("  Starting FavoriteMappers feed...");
//                    List<IPlaylist> playlists = new List<IPlaylist>();
//                    List<IPlaylist> feedPlaylists = new List<IPlaylist>();
//                    JobStats feedStats = new JobStats();
//                    foreach (var mapper in mappers)
//                    {

//                        Logger.log.Info($"  Getting songs by {mapper}...");
//                        playlists.Clear();
//                        foreach (var targetWithPlaylist in jobBuilder.SongTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
//                        {
//                            PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
//                            if (playlistManager != null)
//                            {
//                                if (config.AllBeatSyncSongsPlaylist)
//                                    playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
//                                if (sourceConfig.FavoriteMappers.CreatePlaylist)
//                                {
//                                    IPlaylist feedPlaylist;
//                                    if (sourceConfig.FavoriteMappers.SeparateMapperPlaylists)
//                                        feedPlaylist = playlistManager.GetOrCreateAuthorPlaylist(mapper);
//                                    else
//                                        feedPlaylist = playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSaverFavoriteMappers);
//                                    feedPlaylists.Add(feedPlaylist);
//                                    playlists.Add(feedPlaylist);
//                                }
//                            }
//                        }
//                        FeedResult results = await reader.GetSongsFromFeedAsync(sourceConfig.FavoriteMappers.ToFeedSettings(mapper)).ConfigureAwait(false);
//                        IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
//                        JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
//                        JobStats mapperStats = new JobStats(jobResults);
//                        feedStats += mapperStats;
//                        if (jobs.Any(j => j.Result.Successful) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
//                        {
//                            foreach (var feedPlaylist in feedPlaylists)
//                            {
//                                feedPlaylist.Clear();
//                                feedPlaylist.RaisePlaylistChanged();
//                            }
//                        }
//                        ProcessFinishedJobs(jobs, playlists);

//                        Logger.log.Info($"  Finished getting songs by {mapper}: ({mapperStats}).");
//                    }
//                    sourceStats += feedStats;
//                    Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
//                }
//                else
//                {
//                    Logger.log.Warn("  No FavoriteMappers found, skipping...");
//                }
//            }

//            Logger.log.Info($"  Finished BeatSaver reading: ({sourceStats}).");
//            return sourceStats;
//        }

        


//        public static void ProcessFinishedJobs(IEnumerable<IJob>? jobs, IEnumerable<SongTarget> songTargets, BeatSyncConfig beatSyncConfig, FeedConfigBase feedConfig)
//        {
//            List<IPlaylist> playlists = new List<IPlaylist>();
//            List<IPlaylist> feedPlaylists = new List<IPlaylist>();
//            foreach (var targetWithPlaylist in songTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
//            {
//                PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
//                if (playlistManager != null)
//                {
//                    if (beatSyncConfig.AllBeatSyncSongsPlaylist)
//                        playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
//                    if (feedConfig.CreatePlaylist)
//                    {
//                        IPlaylist feedPlaylist = playlistManager.GetOrAddPlaylist(feedConfig.FeedPlaylist);
//                        playlists.Add(feedPlaylist);
//                        feedPlaylists.Add(feedPlaylist);
//                    }
//                }
//            }
//            if (jobs.Any(j => j.Result.Successful) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
//            {
//                foreach (var feedPlaylist in feedPlaylists)
//                {
//                    feedPlaylist.Clear();
//                    feedPlaylist.RaisePlaylistChanged();
//                }
//            }
//            ProcessFinishedJobs(jobs, playlists);
//        }

//        public static void ProcessFinishedJobs(IEnumerable<IJob>? jobs, IEnumerable<IPlaylist> playlists)
//        {

//            DateTime addedTime = DateTime.Now;
//            TimeSpan offset = new TimeSpan(0, 0, 0, 0, 1);
//            foreach (var job in jobs.Where(j => j.DownloadResult != null && j.DownloadResult.Status != DownloadResultStatus.NetNotFound))
//            {
//                foreach (var playlist in playlists)
//                {
//                    IPlaylistSong? addedSong = playlist.Add(job.Song);
//                    if (addedSong != null)
//                    {
//                        addedSong.DateAdded = addedTime;
//                        addedTime -= offset;
//                    }
//                }
//            }
//            foreach (var playlist in playlists)
//            {
//                playlist.Sort();
//                playlist.RaisePlaylistChanged();
//            }
//        }

//    }
//}
