using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSyncLib.Playlists
{
    public interface IPlaylist
    {
        string Title { get; set; }
        string Author { get; set; }
        string Description { get; set; }
        string FilePath { get; set; }
        Stream GetCoverStream();
        int Count { get; }
        IPlaylistSong[] GetPlaylistSongs();
        void SetCover(byte[] coverImage);
        bool IsDirty { get; }
        void MarkDirty();
        bool AllowDuplicates { get; set; }
        bool TryAdd(IPlaylistSong song);
        bool TryAdd(string songHash, string songName, string songKey, string mapper);
        bool TryRemove(string songHash);
        bool TryRemove(IPlaylistSong song);
        void RemoveDuplicates();
        void Clear();
        bool TryStore();
        bool TryStore(out Exception exception);

    }

    public interface IPlaylist<T> : IPlaylist
        where T : IPlaylistSong, new()
    {
        T[] GetBeatmaps();
        int RemoveAll(Func<T, bool> match);
        bool TryAdd(T song);
    }
}
