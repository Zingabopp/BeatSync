using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Hashing
{
    public interface ISongHashData
    {
        long directoryHash { get; set; }
        string songHash { get; set; }
    }
}
