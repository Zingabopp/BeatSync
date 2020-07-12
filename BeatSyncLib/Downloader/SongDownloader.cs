using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using BeatSyncLib.Configs;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers.ScoreSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public class SongDownloader
    {
        public event EventHandler<string>? SourceStarted;
        public Task<JobStats[]> RunAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager manager)
            => RunAsync(config, jobBuilder, manager, CancellationToken.None);
        public async Task<JobStats[]> RunAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager manager, CancellationToken cancellationToken)
        {
            Task<JobStats>[] downloadTasks = new Task<JobStats>[]
                    {
                        GetBeatSaverAsync(config, jobBuilder, manager, cancellationToken),
                        GetBeastSaberAsync(config, jobBuilder, manager, cancellationToken),
                        GetScoreSaberAsync(config, jobBuilder, manager, cancellationToken)
                    };
            return await Task.WhenAll(downloadTasks).ConfigureAwait(false);
        }

        public static IEnumerable<IJob>? CreateJobs(FeedResult feedResult, IJobBuilder jobBuilder, JobManager jobManager, CancellationToken cancellationToken)
        {
            if (!feedResult.Successful)
                return null;
            if (feedResult.Songs.Count == 0)
            {
                Logger.log?.Info("No songs");
                return Array.Empty<IJob>();
            }
            List<IJob> jobs = new List<IJob>(feedResult.Count);

            foreach (ScrapedSong song in feedResult.Songs.Values)
            {
                Job newJob = jobBuilder.CreateJob(song);
                newJob.RegisterCancellationToken(cancellationToken);
                jobManager.TryPostJob(newJob, out IJob? postedJob);
                if (postedJob != null)
                    jobs.Add(postedJob);
                else
                    Logger.log?.Info($"Posted job is null for {song}, this shouldn't happen.");
            }
            return jobs;
        }

        protected async Task<JobStats> GetScoreSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager, CancellationToken cancellationToken)
        {
            ScoreSaberConfig sourceConfig = config.ScoreSaber;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;
            ScoreSaberReader reader = new ScoreSaberReader();
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.TopRanked, sourceConfig.LatestRanked, sourceConfig.Trending, sourceConfig.TopPlayed };
            if (!feedConfigs.Any(f => f.Enabled))
            {
                Logger.log?.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }
            SourceStarted?.Invoke(this, "ScoreSaber");
            foreach (FeedConfigBase? feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Logger.log?.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                if (results.Successful)
                {
                    IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager, cancellationToken);
                    JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                    JobStats feedStats = new JobStats(jobResults);
                    ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                    Logger.log?.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                    sourceStats += feedStats;
                }
                else
                {
                    if (results.Exception != null)
                    {
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}: {results.Exception.Message}");
                        Logger.log?.Debug(results.Exception);
                    }
                    else
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}: Unknown error.");
                }
            }

            Logger.log?.Info($"  Finished ScoreSaber reading: ({sourceStats}).");
            return sourceStats;
        }

        protected async Task<JobStats> GetBeastSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager, CancellationToken cancellationToken)
        {
            BeastSaberConfig sourceConfig = config.BeastSaber;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;
            BeastSaberReader reader = new BeastSaberReader(sourceConfig.Username, sourceConfig.MaxConcurrentPageChecks);
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Bookmarks, sourceConfig.Follows, sourceConfig.CuratorRecommended };
            if (!feedConfigs.Any(f => f.Enabled))
            {
                Logger.log?.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }

            SourceStarted?.Invoke(this, "BeastSaber");
            foreach (FeedConfigBase? feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                //if (string.IsNullOrEmpty(sourceConfig.Username) && feedConfig.GetType() != typeof(BeastSaberCuratorRecommended))
                //{
                //    Logger.log?.Warn($"  {feedConfig.GetType().Name} feed not available without a valid username.");
                //    continue;
                //}
                Logger.log?.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                if (results.Successful)
                {
                    IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager, cancellationToken);
                    JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                    JobStats feedStats = new JobStats(jobResults);
                    ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                    Logger.log?.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                    sourceStats += feedStats;
                }
                else
                {
                    if (results.Exception != null)
                    {
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}: {results.Exception.Message}");
                        Logger.log?.Debug(results.Exception);
                    }
                    else
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}: Unknown error.");
                }
            }
            Logger.log?.Info($"  Finished BeastSaber reading: ({sourceStats}).");
            return sourceStats;
        }

        protected async Task<JobStats> GetBeatSaverAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager, CancellationToken cancellationToken)
        {
            BeatSaverConfig sourceConfig = config.BeatSaver;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;

            BeatSaverReader reader = new BeatSaverReader();
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Hot, sourceConfig.Downloads, sourceConfig.Latest };
            if (!(feedConfigs.Any(f => f.Enabled) || sourceConfig.FavoriteMappers.Enabled))
            {
                Logger.log?.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }

            SourceStarted?.Invoke(this, "BeatSaver");
            foreach (FeedConfigBase? feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Logger.log?.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings(), cancellationToken).ConfigureAwait(false);
                if (results.Successful)
                {
                    IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager, cancellationToken);
                    JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                    JobStats feedStats = new JobStats(jobResults);
                    ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                    Logger.log?.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                    sourceStats += feedStats;
                }
                else
                {
                    if (results.Exception != null)
                    {
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}{results.Exception.Message}");
                        Logger.log?.Debug($"{results.Exception}");
                    }
                    else
                        Logger.log?.Error($"  Error getting results from {feedConfig.GetType().Name}: Unknown error.");
                }
            }

            string[] mappers = sourceConfig.FavoriteMappers.Mappers ?? Array.Empty<string>();
            if (sourceConfig.FavoriteMappers.Enabled)
            {
                FeedConfigBase feedConfig = sourceConfig.FavoriteMappers;
                if (mappers.Length > 0)
                {
                    Logger.log?.Info("  Starting FavoriteMappers feed...");
                    List<IPlaylist> playlists = new List<IPlaylist>();
                    List<IPlaylist> feedPlaylists = new List<IPlaylist>();
                    List<IPlaylist> recentPlaylists = new List<IPlaylist>();
                    JobStats feedStats = new JobStats();
                    foreach (string? mapper in mappers)
                    {

                        Logger.log?.Info($"  Getting songs by {mapper}...");
                        playlists.Clear();
                        
                        FeedResult results = await reader.GetSongsFromFeedAsync(sourceConfig.FavoriteMappers.ToFeedSettings(mapper)).ConfigureAwait(false);
                        if (results.Successful)
                        {
                            foreach (ITargetWithPlaylists? targetWithPlaylist in jobBuilder.SongTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
                            {
                                PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                                if (playlistManager != null)
                                {
                                    if (config.RecentPlaylistDays > 0)
                                        recentPlaylists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent));
                                    if (config.AllBeatSyncSongsPlaylist)
                                        playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
                                    if (sourceConfig.FavoriteMappers.CreatePlaylist)
                                    {
                                        IPlaylist feedPlaylist;
                                        try
                                        {
                                            if (sourceConfig.FavoriteMappers.SeparateMapperPlaylists)
                                                feedPlaylist = playlistManager.GetOrCreateAuthorPlaylist(mapper);
                                            else
                                                feedPlaylist = playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSaverFavoriteMappers);
                                            feedPlaylists.Add(feedPlaylist);
                                            playlists.Add(feedPlaylist);
                                        }
                                        catch (ArgumentException ex)
                                        {
                                            Logger.log?.Error($"Error getting playlist for FavoriteMappers: {ex.Message}");
                                            Logger.log?.Debug(ex);
                                        }
                                    }
                                }
                            }
                            IEnumerable<IJob> jobs = CreateJobs(results, jobBuilder, jobManager, cancellationToken) ?? Array.Empty<IJob>();
                            JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                            JobStats mapperStats = new JobStats(jobResults);
                            feedStats += mapperStats;
                            if (jobs.Any(j => j.Result?.Successful ?? false) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
                            {
                                // TODO: This should only apply to successful targets.
                                foreach (IPlaylist? feedPlaylist in feedPlaylists)
                                {
                                    feedPlaylist.Clear();
                                    feedPlaylist.RaisePlaylistChanged();
                                }
                            }
                            ProcessFinishedJobs(jobs, playlists, recentPlaylists);

                            Logger.log?.Info($"  Finished getting songs by {mapper}: ({mapperStats}).");
                        }
                        else
                        {
                            if (results.Exception != null)
                            {
                                Logger.log?.Error($"Error getting songs by {mapper}: {results.Exception.Message}");
                                Logger.log?.Debug(results.Exception);
                            }
                            else
                                Logger.log?.Error($"Error getting songs by {mapper}");
                        }
                    }
                    sourceStats += feedStats;
                    Logger.log?.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                }
                else
                {
                    Logger.log?.Warn("  No FavoriteMappers found, skipping...");
                }
            }

            Logger.log?.Info($"  Finished BeatSaver reading: ({sourceStats}).");
            return sourceStats;
        }




        public static void ProcessFinishedJobs(IEnumerable<IJob> jobs, IEnumerable<SongTarget> songTargets, BeatSyncConfig beatSyncConfig, FeedConfigBase feedConfig)
        {
            if(!jobs.Any(j => j.Result?.Successful ?? false))
            {
                return;
            }
            List<IPlaylist> playlists = new List<IPlaylist>();
            List<IPlaylist> feedPlaylists = new List<IPlaylist>();
            List<IPlaylist> recentPlaylists = new List<IPlaylist>();
            foreach (ITargetWithPlaylists? targetWithPlaylist in songTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
            {
                PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                if (playlistManager != null)
                {
                    if (beatSyncConfig.RecentPlaylistDays > 0)
                        recentPlaylists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent));
                    if (beatSyncConfig.AllBeatSyncSongsPlaylist)
                        playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
                    if (feedConfig.CreatePlaylist)
                    {
                        try
                        {
                            IPlaylist feedPlaylist = playlistManager.GetOrAddPlaylist(feedConfig.FeedPlaylist);
                            playlists.Add(feedPlaylist);
                            feedPlaylists.Add(feedPlaylist);
                        }
                        catch (ArgumentException ex)
                        {
                            Logger.log?.Error($"Error getting playlist for FavoriteMappers: {ex.Message}");
                            Logger.log?.Debug(ex);
                        }
                        //catch (PlaylistSerializationException ex) // Caught in GetOrAddPlaylist
                    }
                }
            }
            if (jobs.Any(j => j.Result?.Successful ?? false) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
            {
                foreach (IPlaylist? feedPlaylist in feedPlaylists)
                {
                    // TODO: This should only apply to successful targets.
                    feedPlaylist.Clear();
                    feedPlaylist.RaisePlaylistChanged();
                }
            }
            ProcessFinishedJobs(jobs, playlists, recentPlaylists);
        }

        public static void ProcessFinishedJobs(IEnumerable<IJob> jobs, IEnumerable<IPlaylist> playlists, IEnumerable<IPlaylist> recentPlaylists)
        {
            TimeSpan offset = new TimeSpan(0, 0, 0, 0, 1);
            DateTime addedTime = DateTime.Now - offset;
            foreach (IJob? job in jobs.Where(j => j.DownloadResult != null && j.DownloadResult.Status != DownloadResultStatus.NetNotFound))
            {
                foreach (IPlaylist? playlist in playlists)
                {
                    IPlaylistSong? addedSong = playlist.Add(job.Song);
                    if (addedSong != null)
                    {
                        addedSong.DateAdded = addedTime;
                    }
                }
                DownloadResultStatus downloadStatus = job.Result?.DownloadResult?.Status ?? DownloadResultStatus.Unknown;
                if (downloadStatus == DownloadResultStatus.Success)
                {
                    foreach (IPlaylist? playlist in recentPlaylists)
                    {
                        IPlaylistSong? addedSong = playlist.Add(job.Song);
                        if (addedSong != null)
                        {
                            addedSong.DateAdded = addedTime;
                        }
                    }
                }
            }
            foreach (IPlaylist? playlist in playlists)
            {
                playlist.Sort();
                playlist.RaisePlaylistChanged();
            }
            foreach (IPlaylist? playlist in recentPlaylists)
            {
                playlist.Sort();
                playlist.RaisePlaylistChanged();
            }
        }

    }
}
