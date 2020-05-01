using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeatSyncPlaylists.Legacy
{
    public class LegacyPlaylist : IPlaylist<LegacyPlaylistSong>
    {
        private readonly List<LegacyPlaylistSong> _songs = new List<LegacyPlaylistSong>();

        protected LegacyPlaylist()
        { }

        public IPlaylistSong this[int index]
        {
            get => _songs[index];

            set
            {
                _songs[index] = ConvertFrom(value);
            }
        }

        protected LegacyPlaylistSong ConvertFrom(ISong song)
        {
            if (song is LegacyPlaylistSong legacySong)
                return legacySong;
            return new LegacyPlaylistSong(song);
        }

        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string Filename { get; set; } = string.Empty;
        public bool IsDirty { get; set; }
        public bool AllowDuplicates { get; set; }
        public int Count => _songs.Count;
        public bool IsReadOnly => false;

        public void Add(ISong song)
        {
            if (AllowDuplicates || (!_songs.Any(s => s.Hash == song.Hash || s.Key == song.Key)))
            {
                _songs.Add(ConvertFrom(song));
                MarkDirty();
            }
        }

        public void Add(string songHash, string? songName, string? songKey, string? mapper) =>
            Add(new LegacyPlaylistSong(songHash, songName, songKey, mapper));

        public void Add(IPlaylistSong item) => Add((ISong)item);

        public void Clear()
        {
            _songs.Clear();
            MarkDirty();
        }

        public bool Contains(IPlaylistSong item)
        {
            return _songs.Any(s => s.Equals(item));
        }

        public void CopyTo(IPlaylistSong[] array, int arrayIndex)
        {
            int index = arrayIndex;
            foreach (var song in _songs)
            {
                array[index] = song;
                index++;
            }
        }

        public Stream GetCoverStream()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IPlaylistSong> GetEnumerator()
        {
            return _songs.GetEnumerator();
        }

        public int IndexOf(IPlaylistSong item)
        {
            if (item is LegacyPlaylistSong legacySong)
                return _songs.IndexOf(legacySong);
            else
                return -1;
        }

        public void Insert(int index, IPlaylistSong item)
        {
            _songs.Insert(index, ConvertFrom(item));
            MarkDirty();
        }

        public void MarkDirty(bool dirty = true)
        {
            IsDirty = dirty;
        }

        public bool Remove(IPlaylistSong item)
        {
            bool songRemoved = false;
            if (item is LegacyPlaylistSong legacySong)
                songRemoved = _songs.Remove(legacySong);
            else
            {
                LegacyPlaylistSong song = _songs.FirstOrDefault(s => s.Equals(item));
                if (song != null)
                    songRemoved = _songs.Remove(song);
            }
            if (songRemoved)
                MarkDirty();
            return songRemoved;
        }

        public int RemoveAll(Func<LegacyPlaylistSong, bool> match)
        {
            int removedSongs = 0;
            if (match != null)
                removedSongs = _songs.RemoveAll(s => match(s));
            if (removedSongs > 0)
                MarkDirty();
            return removedSongs;
        }

        public void RemoveAt(int index)
        {
            _songs.RemoveAt(index);
            MarkDirty();
        }

        public void RemoveDuplicates()
        {
            throw new NotImplementedException();
        }

        public void SetCover(byte[] coverImage)
        {
            throw new NotImplementedException();
        }

        public void SetCover(string coverImageStr)
        {
            throw new NotImplementedException();
        }

        public void SetCover(Stream stream)
        {
            throw new NotImplementedException();
        }

        public bool TryRemoveByHash(string songHash)
        {
            songHash = songHash.ToUpper();
            return RemoveAll(s => s.Hash == songHash) > 0;
        }

        public bool TryRemoveByKey(string songKey)
        {
            songKey = songKey.ToLower();
            return RemoveAll(s => s.Key == songKey) > 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _songs.GetEnumerator();
        }
    }
}
