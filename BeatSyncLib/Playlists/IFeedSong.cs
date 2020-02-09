using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Playlists
{
    public interface IFeedSong : IPlaylistSong
    {
        HashSet<string> FeedSources { get; }
        void AddFeedSource(string sourceName);
    }
}
