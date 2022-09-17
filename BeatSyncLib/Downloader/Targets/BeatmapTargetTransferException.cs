using System;

namespace BeatSyncLib.Downloader.Targets
{
    public class BeatmapTargetTransferException : Exception
    {
        public BeatmapTargetTransferException()
            : base("An error occurred transferring the download to the target.")
        { }

        public BeatmapTargetTransferException(string message)
            : base(message)
        { }

        public BeatmapTargetTransferException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}
