using SongFeedReaders.Data;
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
        void SetCover(string coverImageStr);
        void SetCover(Stream stream);
        bool IsDirty { get; }
        void MarkDirty();
        bool AllowDuplicates { get; set; }
        bool TryAdd(IPlaylistSong song);
        bool TryAdd(ISong song);
        bool TryAdd(string songHash, string songName, string songKey, string mapper);
        bool TryRemove(string songHashOrKey);
        bool TryRemove(IPlaylistSong song);
        void RemoveDuplicates();
        void Clear();
        bool TryStore();
        bool TryStore(out Exception exception);
        /// <summary>
        /// Populates the playlist with information from the provided file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="updatePath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        void PopulateFromFile(string filePath, bool updatePath = true);

    }

    public interface IPlaylist<T> : IPlaylist
        where T : IPlaylistSong, new()
    {
        T[] GetBeatmaps();
        int RemoveAll(Func<T, bool> match);
        bool TryAdd(T song);
    }
}
