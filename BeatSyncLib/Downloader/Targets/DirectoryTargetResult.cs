using System;
using BeatSyncLib.Filtering;
using BeatSyncLib.Utilities;
using SongFeedReaders.Models;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTargetResult : TargetResult
    {
        public ZipExtractResult? ZipExtractResult { get; protected set; }
        public DirectoryTargetResult(ISong beatmap, DirectoryTarget target, BeatmapState songState,
            bool success, string? beatmapHash, Exception? exception)
            : base(beatmap, target, songState, success, beatmapHash, exception)
        { }

        public DirectoryTargetResult(ISong beatmap, DirectoryTarget target, BeatmapState songState,
            bool success, string? beatmapHash, ZipExtractResult? zipExtractResult, Exception? exception)
            : base(beatmap, target, songState, success, beatmapHash, exception)
        {
            ZipExtractResult = zipExtractResult;
        }
    }
}