using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using SongFeedReaders.Data;
using BeatSyncLib.Downloader;
using BeatSyncLib.History;
using BeatSyncLib.Downloader.Targets;

namespace BeatSyncConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ScrapedSong song = null;
            DownloadManager manager = new DownloadManager(3);
            var cts = new CancellationTokenSource();
            manager.Start(cts.Token);
            IDownloadJob job = new DownloadJob(song, "Temp");
            ISongTargetFactorySettings targetFactorySettings = new DirectoryTargetFactorySettings() { OverwriteTarget = true };
            ISongTargetFactory targetFactory = new DirectoryTargetFactory("Songs", targetFactorySettings);
            ISongTarget target = targetFactory.CreateTarget(song);
            job.AddDownloadFinishedCallback(async c =>
            {
                HistoryEntry entry;
                if (c.DownloadResult.Status == DownloadResultStatus.Success)
                {
                    TargetResult targetResult = await target.TransferAsync(c.DownloadResult.DownloadContainer.GetResultStream());

                    if (targetResult.Success)
                    {
                        entry = new HistoryEntry(c.SongHash, c.SongName, c.LevelAuthorName, HistoryFlag.Downloaded);
                        // Add to playlist
                    }
                    else
                        entry = new HistoryEntry(c.SongHash, c.SongName, c.LevelAuthorName, HistoryFlag.Error);
                }
                else
                    entry = c.ToFailedHistoryEntry();
                // Add entry to history.
            });

            manager.TryPostJob(job, out _);


        }
    }
}
