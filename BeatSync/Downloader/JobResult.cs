using BeatSync.Playlists;
using BeatSync.Utilities;
using System;

namespace BeatSync.Downloader
{
    public class JobResult
    {
        public bool Successful
        {
            get
            {
                if (DownloadResult == null || ZipResult == null)
                    return false;
                if (DownloadResult.Status != DownloadResultStatus.Success)
                    return false;
                if (ZipResult.ResultStatus != ZipExtractResultStatus.Success)
                    return false;
                return true;
            }
        }
        public string SongDirectory { get; set; }
        public string BeatSaverHash { get; set; }
        public string HashAfterDownload { get; set; }
        public DownloadResult DownloadResult { get; set; }
        public ZipExtractResult ZipResult { get; set; }
        public PlaylistSong Song { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return $"{Song?.ToString()}, Download Status: {DownloadResult?.Status}, Zip Result: {ZipResult?.ResultStatus}";
        }
    }
}
