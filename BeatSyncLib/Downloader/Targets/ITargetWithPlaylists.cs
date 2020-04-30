using BeatSyncLib.History;
using BeatSyncLib.Playlists;
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
