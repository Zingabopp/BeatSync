﻿using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Filtering;

namespace BeatSyncLib.Downloader.Targets
{
    public class TargetResult
    {
        public IBeatmapTarget Target { get; }
        public ISong Beatmap { get; }
        public bool Success { get; protected set; }
        public BeatmapState SongState { get; }
        public string? BeatmapHash { get; }
        public Exception? Exception { get; protected set; }
        public TargetResult(ISong beatmap, IBeatmapTarget target, BeatmapState songState, 
            bool success, string? beatmapHash, Exception? exception)
        {
            Beatmap = beatmap;
            Target = target;
            Success = success;
            SongState = songState;
            BeatmapHash = beatmapHash;
            Exception = exception;
        }
    }
}
