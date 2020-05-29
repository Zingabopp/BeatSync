using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Configs
{
    public interface ISongLocation
    {
        bool Enabled { get; set; }
        string BasePath { get; set; }
        string SongsDirectory { get; }
        string? PlaylistDirectory { get; }
        string HistoryPath { get; }

        bool IsValid();
        bool IsValid(out string? reason);
    }
}
