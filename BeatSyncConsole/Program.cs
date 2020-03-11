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
            DownloadJob job = new DownloadJob(song, "Temp");
            ISongTarget target = new DirectoryTarget("Songs", song, true);
            job.AddDownloadFinishedCallback(c =>
            {
                if(c.DownloadResult.Status != DownloadResultStatus.Success)
                {
                    HistoryEntry entry = c.ToFailedHistoryEntry();
                    // Add entry to history.
                    return;
                }
                target.TransferAsync(c.DownloadResult.DownloadContainer.GetResultStream()).ContinueWith(async t =>
                {
                    TargetResult targetResult = await t.ConfigureAwait(false);
                    HistoryEntry entry;
                    if (targetResult.Success)
                    {
                        entry = new HistoryEntry(c.SongHash, c.SongName, c.LevelAuthorName, HistoryFlag.Downloaded);
                    }
                    else
                        entry = new HistoryEntry(c.SongHash, c.SongName, c.LevelAuthorName, HistoryFlag.Error);
                    // Add entry to history.
                });
            });
            manager.TryPostJob(job, out _);
            

        }
    }
}
