using SongFeedReaders.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSyncPlaylists
{
    public abstract class Playlist<T> : IPlaylist<T>
        where T : IPlaylistSong, new()
    {
        protected abstract IList<T> _songs { get; set; }
        public IPlaylistSong this[int index]
        {
            get => _songs[index];

            set
            {
                _songs[index] = ConvertFrom(value);
            }
        }

        public abstract int RemoveAll(Func<T, bool> match);

        public abstract string Title { get; set; }
        public abstract string? Author { get; set; }
        public abstract string? Description { get; set; }
        public abstract string Filename { get; set; }
        public string? SuggestedExtension { get; set; }
        public bool IsDirty { get; set; }
        public bool AllowDuplicates { get; set; }
        public int Count => _songs.Count;
        public virtual bool IsReadOnly => false;

        protected abstract T ConvertFrom(ISong song);

        public virtual void Add(ISong song)
        {
            if (AllowDuplicates || (!_songs.Any(s => s.Hash == song.Hash || s.Key == song.Key)))
            {
                _songs.Add(ConvertFrom(song));
                MarkDirty();
            }
        }

        public virtual void Add(string songHash, string? songName, string? songKey, string? mapper) =>
            Add(new T() {
                Hash = songHash, 
                Name = songName,
                Key =  songKey,
                LevelAuthorName = mapper 
            });

        public virtual void Add(IPlaylistSong item) => Add((ISong)item);

        public virtual void Clear()
        {
            _songs.Clear();
            MarkDirty();
        }

        public virtual bool Contains(IPlaylistSong item)
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

        public abstract Stream GetCoverStream();

        public IEnumerator<T> GetEnumerator()
        {
            return _songs.GetEnumerator();
        }

        public int IndexOf(IPlaylistSong item)
        {
            if (item is T song)
                return _songs.IndexOf(song);
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
            if (item is T matchedType)
                songRemoved = _songs.Remove(matchedType);
            else
            {
                T song = _songs.FirstOrDefault(s => s.Equals(item));
                if (song != null)
                    songRemoved = _songs.Remove(song);
            }
            if (songRemoved)
                MarkDirty();
            return songRemoved;
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

        public abstract void SetCover(byte[] coverImage);

        public abstract void SetCover(string? coverImageStr);

        public abstract void SetCover(Stream stream);

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

        IEnumerator<IPlaylistSong> IEnumerable<IPlaylistSong>.GetEnumerator()
        {
            var thing = (IList<IPlaylistSong>)_songs;
            return thing.GetEnumerator();
        }
    }
}
