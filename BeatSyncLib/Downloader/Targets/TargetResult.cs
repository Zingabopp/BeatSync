using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader.Targets
{
    public class TargetResult
    {
        public SongTarget Target { get; }
        public bool Success { get; protected set; }
        public SongState SongState { get; }
        public Exception? Exception { get; protected set; }
        public TargetResult(SongTarget target, SongState songState, bool success, Exception? exception)
        {
            Target = target;
            Success = success;
            SongState = songState;
            Exception = exception;
        }
    }
}
