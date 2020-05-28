using BeatSyncLib.History;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader.Targets
{
    public interface ITargetWithHistory
    {
        HistoryManager? HistoryManager { get; }
    }
}
