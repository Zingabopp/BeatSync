using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.History;
using SongFeedReaders.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace BeatSyncConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper());
            ScrapedSong song = new ScrapedSong("19f2879d11a91b51a5c090d63471c3e8d9b7aee3", "Believer", "Rustic", "b");
            DownloadManager manager = new DownloadManager(3);
            string tempDirectory = "Temp";
            string songsDirectory = "Songs";
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(songsDirectory);
            CancellationTokenSource cts = new CancellationTokenSource();
            manager.Start(cts.Token);
            //DownloadContainer downloadContainer = new DownloadMemoryContainer();
            DownloadContainer downloadContainer = new DownloadFileContainer(Path.Combine(tempDirectory, (song.SongKey ?? song.Hash) + ".zip"));
            downloadContainer.ProgressChanged += (sender, progress) =>
            {
                Console.WriteLine(progress);
            };
            IDownloadJob job = new DownloadJob(song, downloadContainer);
            ISongTargetFactorySettings targetFactorySettings = new DirectoryTargetFactorySettings() { OverwriteTarget = false };
            ISongTargetFactory targetFactory = new DirectoryTargetFactory(songsDirectory, targetFactorySettings);
            ISongTarget target = targetFactory.CreateTarget(song);
            job.AddDownloadFinishedCallback(async c =>
            {
                HistoryEntry entry;
                if (c.DownloadResult.Status == DownloadResultStatus.Success)
                {
                    TargetResult targetResult = null;
                    try
                    {
                        using (Stream data = c.DownloadResult.DownloadContainer.GetResultStream())
                            targetResult = await target.TransferAsync(data).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        entry = new HistoryEntry(c.SongHash, c.SongName, c.LevelAuthorName, HistoryFlag.Error);
                    }

                    if (targetResult?.Success ?? false)
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
            try
            {
                await manager.CompleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Press any key to continue...");
            Console.Read();
        }
    }
}
