using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader
{
    public enum JobStage
    {
        NotStarted = 0,
        Downloading = 1,
        TransferringToTargets = 2,
        Finishing = 3,
        Finished = 4
    }

    public enum JobState
    {
        NotReady = 0,
        Ready = 1,
        Running = 2,
        Finished = 3,
        Cancelled = 4,
        Error = 5
    }

    public enum JobProgressType
    {
        None = 0,
        StageProgress = 1,
        StageCompletion = 2,
        Finished = 3,
        Error = 4,
        Cancellation = 5,
        Paused = 6
    }
}
