using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSync.Playlists;
using BeatSync.Utilities;

namespace BeatSync.Downloader
{
    public class JobResult
    {
        public bool Successful { get; set; }
        public string SongDirectory { get; set; }
        public string BeatSaverHash { get; set; }
        public string HashAfterDownload { get; set; }
        public DownloadResult DownloadResult { get; set; }
        public ZipExtractResult ZipResult { get; set; }
        public PlaylistSong Song { get; set; }

        public override string ToString()
        {
            return $"{Song?.ToString()}, Download Status: {DownloadResult?.Status}, Zip Result: {ZipResult?.ResultStatus}";
        }
    }
}
