using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SongFeedReaders;

namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        public static BeatSync Instance { get; set; }
        public static bool PauseWork { get; set; }

        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
            Logger.log.Warn("BeatSync Awake");
        }
        public void Start()
        {
            Logger.log.Debug("BeatSync Start()");
            StartCoroutine(ScrapeSongsCoroutine());
        }

        public IEnumerator<WaitForSeconds> ScrapeSongsCoroutine()
        {
            yield return null;
            Logger.log.Debug("Starting ScrapeSongsCoroutine");
            var beatSaverReader = new SongFeedReaders.BeatSaverReader();
            var bsaberReader = new BeastSaberReader("Zingabopp", 5);
            var scoreSaberReader = new ScoreSaberReader();
            Logger.log.Warn($"BS: {beatSaverReader != null}, BSa: {bsaberReader != null}, SS: {scoreSaberReader != null}");
            var beatSaverTask = Task.Run(() => beatSaverReader.GetSongsFromFeedAsync(new BeatSaverFeedSettings((int)BeatSaverFeed.Hot) { MaxSongs = 70 }));
            var bsaberTask = Task.Run(() => bsaberReader.GetSongsFromFeedAsync(new BeastSaberFeedSettings(0) { MaxSongs = 70 }));
            var scoreSaberTask = Task.Run(() => scoreSaberReader.GetSongsFromFeedAsync(new ScoreSaberFeedSettings(0) { MaxSongs = 70 }));
            TestPrintReaderResults(beatSaverTask, bsaberTask, scoreSaberTask);

        }

        public void PrintSongs(string source, IEnumerable<ScrapedSong> songs)
        {
            foreach (var song in songs)
            {
                Logger.log.Warn($"{source}: {song.SongName} by {song.MapperName}, hash: {song.Hash}");
            }
        }

        public async Task TestPrintReaderResults(Task<Dictionary<string, ScrapedSong>> beatSaverTask,
            Task<Dictionary<string, ScrapedSong>> bsaberTask,
            Task<Dictionary<string, ScrapedSong>> scoreSaberTask)
        {
            await Task.WhenAll(beatSaverTask, bsaberTask, scoreSaberTask).ConfigureAwait(false);


            Logger.log.Info($"{beatSaverTask.Status.ToString()}");
            try
            {
                if (beatSaverTask.IsCompleted)
                    PrintSongs("BeatSaver", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            try
            {
                if (bsaberTask.IsCompleted)
                    PrintSongs("BeastSaber", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            try
            {
                if (scoreSaberTask.IsCompleted)
                    PrintSongs("ScoreSaber", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            Logger.log.Warn("Continuing loop");
        }
    }
}
