using SongFeedReaders.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSyncPlaylists
{
    /// <summary>
    /// Base class for a Playlist.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Playlist<T> : IPlaylist<T>
        where T : IPlaylistSong, new()
    {
        /// <summary>
        /// List of songs in the playlist.
        /// </summary>
        protected abstract IList<T> _songs { get; set; }

        /// <inheritdoc/>
        public IPlaylistSong this[int index]
        {
            get => _songs[index];

            set
            {
                _songs[index] = ConvertFrom(value);
            }
        }

        /// <inheritdoc/>
        public event EventHandler? PlaylistChanged;

        /// <summary>
        /// Removes all playlists that match the provided delegate.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public abstract int RemoveAll(Func<T, bool> match);

        /// <inheritdoc/>
        public abstract string Title { get; set; }
        /// <inheritdoc/>
        public abstract string? Author { get; set; }
        /// <inheritdoc/>
        public abstract string? Description { get; set; }
        /// <inheritdoc/>
        public abstract string Filename { get; set; }
        /// <inheritdoc/>
        public string? SuggestedExtension { get; set; }
        /// <inheritdoc/>
        public bool AllowDuplicates { get; set; }
        /// <inheritdoc/>
        public int Count => _songs.Count;
        /// <inheritdoc/>
        public virtual bool IsReadOnly => false;

        protected abstract T ConvertFrom(ISong song);

        /// <inheritdoc/>
        public virtual IPlaylistSong? Add(ISong song)
        {
            if (AllowDuplicates || (!_songs.Any(s => s.Hash == song.Hash || s.Key == song.Key)))
            {
                T playlistSong = ConvertFrom(song);
                _songs.Add(playlistSong);
                return playlistSong;
            }
            return null;
        }

        /// <inheritdoc/>
        public virtual IPlaylistSong? Add(string songHash, string? songName, string? songKey, string? mapper) =>
            Add(new T() {
                Hash = songHash, 
                Name = songName,
                Key =  songKey,
                LevelAuthorName = mapper 
            });

        /// <inheritdoc/>
        public virtual IPlaylistSong? Add(IPlaylistSong item) => Add((ISong)item);

        /// <inheritdoc/>
        public virtual void Clear()
        {
            _songs.Clear();
        }

        /// <inheritdoc/>
        public virtual void Sort()
        {
            _songs = _songs.OrderByDescending(s => s.DateAdded).ToList();
        }

        /// <inheritdoc/>
        public virtual bool Contains(IPlaylistSong item)
        {
            return _songs.Any(s => s.Equals(item));
        }

        /// <inheritdoc/>
        public void CopyTo(IPlaylistSong[] array, int arrayIndex)
        {
            int index = arrayIndex;
            foreach (var song in _songs)
            {
                array[index] = song;
                index++;
            }
        }

        /// <inheritdoc/>
        public abstract Stream GetCoverStream();

        public IEnumerator<T> GetEnumerator()
        {
            return _songs.GetEnumerator();
        }

        /// <inheritdoc/>
        public int IndexOf(IPlaylistSong item)
        {
            if (item is T song)
                return _songs.IndexOf(song);
            else
                return -1;
        }

        /// <inheritdoc/>
        public void Insert(int index, IPlaylistSong item)
        {
            _songs.Insert(index, ConvertFrom(item));
        }

        /// <inheritdoc/>
        public void RaisePlaylistChanged()
        {
            EventHandler? handler = PlaylistChanged;
            handler?.Invoke(this, null);
        }

        /// <inheritdoc/>
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
            return songRemoved;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            _songs.RemoveAt(index);
        }

        /// <inheritdoc/>
        public void RemoveDuplicates()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public abstract void SetCover(byte[] coverImage);

        /// <inheritdoc/>
        public abstract void SetCover(string? coverImageStr);

        /// <inheritdoc/>
        public abstract void SetCover(Stream stream);

        /// <inheritdoc/>
        public bool TryRemoveByHash(string songHash)
        {
            songHash = songHash.ToUpper();
            return RemoveAll(s => s.Hash == songHash) > 0;
        }

        /// <inheritdoc/>
        public bool TryRemoveByKey(string songKey)
        {
            songKey = songKey.ToLower();
            return RemoveAll(s => s.Key == songKey) > 0;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _songs.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator<IPlaylistSong> IEnumerable<IPlaylistSong>.GetEnumerator()
        {
            var thing = (IList<IPlaylistSong>)_songs;
            return thing.GetEnumerator();
        }

        /// <inheritdoc/>
        void ICollection<IPlaylistSong>.Add(IPlaylistSong item)
        {
            Add(item);
        }
    }
}
