using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader
{
    public interface ISongTarget
    {
        string TargetName { get; }





    }
    public enum TargetSource
    {
        None = 0,
        Zip = 1 << 0,
        Directory = 1 << 1
    }
}
