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

        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
            Logger.log.Warn("BeatSync Awake");
        }
        public void Start()
        {
            Logger.log.Warn("BeatSync Start()");
            StartCoroutine(GetSongsCoRoutine());
        }

        public IEnumerator<WaitForSeconds> GetSongsCoRoutine()
        {
            Logger.log.Warn("Starting GetSongsCoRoutine");
            var beatSaverReader = new SongFeedReaders.BeatSaverReader();
            var beatSaverTask = Task.Run(() => beatSaverReader.GetSongsFromFeedAsync(new BeatSaverFeedSettings((int)BeatSaverFeed.Hot) { MaxSongs = 70 }));
            var bsaberReader = new BeastSaberReader("Zingabopp", 5);
            var bsaberTask = Task.Run(() => bsaberReader.GetSongsFromFeedAsync(new BeastSaberFeedSettings(0) { MaxSongs = 70 }));
            var scoreSaberReader = new ScoreSaberReader();
            var scoreSaberTask = Task.Run(() => scoreSaberReader.GetSongsFromFeedAsync(new ScoreSaberFeedSettings(0) { MaxSongs = 70 }));
            Logger.log.Warn("Entering loop");
            while (!(beatSaverTask.IsCompleted && bsaberTask.IsCompleted && scoreSaberTask.IsCompleted))
            {
                Logger.log.Info($"{beatSaverTask.Status.ToString()}");
                yield return new WaitForSeconds(2);
                try
                {
                    if (beatSaverTask.IsCompleted)
                        PrintSongs("BeatSaver", beatSaverTask.Result.Values);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
                try
                {
                    if (bsaberTask.IsCompleted)
                        PrintSongs("BeastSaber", bsaberTask.Result.Values);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
                try
                {
                    if (scoreSaberTask.IsCompleted)
                        PrintSongs("ScoreSaber", scoreSaberTask.Result.Values);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
                Logger.log.Warn("Continuing loop");
            }
        }

        public void PrintSongs(string source, IEnumerable<ScrapedSong> songs)
        {
            foreach (var song in songs)
            {
                Logger.log.Warn($"{source}: {song.SongName} by {song.MapperName}, hash: {song.Hash}");
            }
        }

    }
}
