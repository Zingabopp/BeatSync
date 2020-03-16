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
        public static IJobBuilder CreateJobBuilder()
        {
            string tempDirectory = "Temp";
            string songsDirectory = "Songs";
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(songsDirectory);
            IDownloadJobFactory downloadJobFactory = new DownloadJobFactory(song =>
            {
                // return new DownloadMemoryContainer();
                return new DownloadFileContainer(Path.Combine(tempDirectory, (song.SongKey ?? song.Hash) + ".zip"));
            });
            ISongTargetFactorySettings targetFactorySettings = new DirectoryTargetFactorySettings() { OverwriteTarget = false };
            ISongTargetFactory targetFactory = new DirectoryTargetFactory(songsDirectory, targetFactorySettings);
            JobFinishedCallback jobFinishedCallback = new JobFinishedCallback(async c =>
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                // Add entry to history.
                if (c.Successful)
                {
                    // Add song to playlist.
                    Console.WriteLine($"Job completed successfully: {c.Song}");
                }
                else
                { 
                    Console.WriteLine($"Job failed: {c.Song}");
                }
            });
            return new JobBuilder()
                .SetDownloadJobFactory(downloadJobFactory)
                .AddTargetFactory(targetFactory)
                .SetDefaultJobFinishedCallback(jobFinishedCallback);

        }

        static async Task Main(string[] args)
        {
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            ScrapedSong song = new ScrapedSong("19f2879d11a91b51a5c090d63471c3e8d9b7aee3", "Believer", "Rustic", "b");
            DownloadManager manager = new DownloadManager(3);
            CancellationTokenSource cts = new CancellationTokenSource();
            manager.Start(cts.Token);
            IJobBuilder jobBuilder = CreateJobBuilder();
            Job job = jobBuilder.CreateJob(song);
            int stageUpdates = 0;
            job.ProgressChanged += (s, p) =>
            {
                Job j = (Job)s;
                if (p.JobProgressType == JobProgressType.StageProgress)
                    stageUpdates++;
                if (stageUpdates > 4)
                    cts.Cancel();
                Console.WriteLine($"Progress on {j}: {p}");
            };
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

        public async Task Thing()
        {
            IJobBuilder jobBuilder = null;
            int c;
            JobFinishedCallback jobFinishedCallback = new JobFinishedCallback(async c =>
            {
                await Task.Delay(500);
            });
            Func<int, Task> callback = async (c) =>
            {

                await Task.Delay(500);
                return;
            };

            jobBuilder.SetDefaultJobFinishedCallback(async c =>
            {
                await Task.Delay(500);
            });


        }
    }
}
