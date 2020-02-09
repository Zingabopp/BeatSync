using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Playlists
{
    public interface IPlaylistSong : IEquatable<IPlaylistSong>
    {
        string Hash { get; set; }
        string Key { get; set; }
        string LevelAuthorName { get; set; }
        string Name { get; set; }
        DateTime? DateAdded { get; set; }
    }

    public interface IPlaylistSong<T> : IEquatable<T>
    {

    }
}
