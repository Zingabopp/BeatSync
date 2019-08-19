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
        public static bool PauseWork { get; set; }
        private SongDownloader Downloader;
        
        

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
            Downloader = new SongDownloader(Plugin.config.Value);
            //LoadCachedSongHashesAsync(Plugin.CachedHashDataPath);
            //Logger.log?.Critical($"Read {HashDictionary.Count} cached songs.");
            //var hashTask = Task.Run(() => AddMissingHashes());
            //Logger.log?.Info("Converting legacy playlists.");
            //PlaylistManager.ConvertLegacyPlaylists();
            FavoriteMappers.Initialize();
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
            //TestPrintReaderResults(beatSaverTask, bsaberTask, scoreSaberTask);

        }

        

        
        
    }
}

