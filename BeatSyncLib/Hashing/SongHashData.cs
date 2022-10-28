using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Hashing
{
    public struct SongHashData
    {
        public long directoryHash { get; set; }
        public string? songHash { get; set; }
    }
}
