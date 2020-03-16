using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.History;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
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
                return new DownloadFileContainer(Path.Combine(tempDirectory, (song.Key ?? song.Hash) + ".zip"));
            });
            ISongTargetFactorySettings targetFactorySettings = new DirectoryTargetFactorySettings() { OverwriteTarget = false };
            ISongTargetFactory targetFactory = new DirectoryTargetFactory(songsDirectory, targetFactorySettings);
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async c =>
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
                .SetDefaultJobFinishedAsyncCallback(jobFinishedCallback);

        }

        static async Task Main(string[] args)
        {
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            List<ISong> songs = new List<ISong>();
            ISong song = new ScrapedSong("19f2879d11a91b51a5c090d63471c3e8d9b7aee3", "Believer", "Rustic", "b");
            songs.Add(song);
            songs.Add(new ScrapedSong("fb557e1746a49376473d7d40159aa6290d4a9b66", "ruckyshit", "ruckus", "7ef5"));
            songs.Add(new ScrapedSong("68496811309fe62303edde686eb160f8e45aa9ce", "The Thrill (Porter Robinson Remix)", "ruckus", "4a4c"));
            songs.Add(new ScrapedSong("b44d11fc21debe3f227b582e22e200095ba82aa7", "Silver Moon", "ruckus", "650"));
            //songs.Add(new ScrapedSong("660e2e200b317460ef1a64b66347d54dc4df396e", "Ursa Minor |Electron Mix|", "ruckus", "bcc"));
            DownloadManager manager = new DownloadManager(3);
            CancellationTokenSource cts = new CancellationTokenSource();
            manager.Start(cts.Token);
            IJobBuilder jobBuilder = CreateJobBuilder();
            HashSet<IJob> runningJobs = new HashSet<IJob>();
            int completedJobs = 0;
            foreach (ISong songToAdd in songs)
            {
                IJob job = jobBuilder.CreateJob(songToAdd);
                job.JobProgressChanged += (s, p) =>
                {
                    IJob j = (IJob)s;
                    runningJobs.Add(j);

                    //if (stageUpdates > 4)
                    //    cts.Cancel();
                    if (p.JobProgressType == JobProgressType.Finished)
                    {
                        int finished = ++completedJobs;
                        Console.WriteLine($"({finished} finished) Completed {j}: {p}");
                    }
                    else
                        Console.WriteLine($"({runningJobs.Count} jobs seen) Progress on {j}: {p}");
                };
                if (!manager.TryPostJob(job, out IJob j))
                {
                    Console.WriteLine($"Couldn't post duplicate: {j}");
                }
            }

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
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async c =>
            {
                await Task.Delay(500);
            });
            Func<int, Task> callback = async (c) =>
            {

                await Task.Delay(500);
                return;
            };

            jobBuilder.SetDefaultJobFinishedAsyncCallback(async c =>
            {
                await Task.Delay(500);
            });


        }
    }
}
