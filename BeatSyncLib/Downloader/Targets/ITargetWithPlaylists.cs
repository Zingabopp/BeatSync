using BeatSyncLib.History;
using BeatSyncPlaylists;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader.Targets
{
    public interface ITargetWithPlaylists
    {
        PlaylistManager? PlaylistManager { get; }
    }
}
