using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SongFeedReaders;
using BeatSync.Configs;
using Newtonsoft.Json;
using SongCore.Data;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;
using BeatSync.Playlists;
using BeatSync.Utilities;
using SongFeedReaders.DataflowAlternative;
using System.IO.Compression;

namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        public static BeatSync Instance { get; set; }
        private static bool _paused;
        public static bool Paused
        {
            get
            {
                return _paused;
            }
            set
            {
                _paused = value;
                if (_paused)
                    SongFeedReaders.Utilities.Pause();
                else
                    SongFeedReaders.Utilities.UnPause();
            }
        }

        private SongDownloader Downloader;
        private SongHasher SongHasher;
        private HistoryManager HistoryManager;


        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
            //FinishedHashing += OnHashingFinished;
        }


        public void Start()
        {
            Logger.log?.Debug("BeatSync Start()");
            
            SongHasher = new SongHasher(Plugin.CustomLevelsPath, Plugin.CachedHashDataPath);
            HistoryManager = new HistoryManager(Path.Combine(Plugin.UserDataPath, "BeatSyncHistory.json"));
            Task.Run(() => HistoryManager.Initialize());
            Downloader = new SongDownloader(Plugin.config.Value, HistoryManager, SongHasher, CustomLevelPathHelper.customLevelsDirectoryPath);
            //LoadCachedSongHashesAsync(Plugin.CachedHashDataPath);
            //Logger.log?.Critical($"Read {HashDictionary.Count} cached songs.");
            //var hashTask = Task.Run(() => AddMissingHashes());
            //Logger.log?.Info("Converting legacy playlists.");
            //PlaylistManager.ConvertLegacyPlaylists();
            StartCoroutine(HashSongsCoroutine());
            FavoriteMappers.Initialize();
        }

        public IEnumerator<WaitUntil> HashSongsCoroutine()
        {
            SongHasher.LoadCachedSongHashes();
            var hashTask = Task.Run(() => SongHasher.AddMissingHashes());
            var hashWait = new WaitUntil(() => hashTask.IsCompleted);
            yield return hashWait;
            StartCoroutine(ScrapeSongsCoroutine());
        }


        public IEnumerator<WaitUntil> ScrapeSongsCoroutine()
        {
            Logger.log?.Debug("Starting ScrapeSongsCoroutine");
            var readTask = Downloader.RunReaders();
            var readWait = new WaitUntil(() => readTask.IsCompleted);
            yield return readWait;
            var downloadTask = Downloader.RunDownloaderAsync();
            var downloadWait = new WaitUntil(() => downloadTask.IsCompleted);
            yield return downloadWait;
            HistoryManager.WriteToFile();
            //TestPrintReaderResults(beatSaverTask, bsaberTask, scoreSaberTask);

        }





    }
}

