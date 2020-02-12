using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BeatSyncLib.Playlists.Blister
{
    public class BlisterPlaylist : IPlaylist<BlisterPlaylistSong>
    {
        public static readonly string[] FileExtensions = new string[] { "blist" };

        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("cover")]
        protected byte[] Cover { get; set; }

        [JsonProperty("maps")]
        protected List<BlisterPlaylistSong> Beatmaps { get; set; }
        public BlisterPlaylistSong[] GetBeatmaps() => Beatmaps.ToArray();
        public string FilePath { get; set; }

        public int Count => Beatmaps?.Count ?? 0;

        public bool IsDirty { get; protected set; }

        public bool AllowDuplicates { get; set; }


        public Stream GetCoverStream()
        {
            if (Cover == null)
                return null;
            return new MemoryStream(Cover);
        }

        public IPlaylistSong[] GetPlaylistSongs()
        {
            return Beatmaps.ToArray<IPlaylistSong>();
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void SetCover(string base64Str)
        {
            Cover = Utilities.Util.StringToByteArray(base64Str);
        }

        public void SetCover(byte[] coverImage)
        {
            if (Cover == coverImage)
                return;
            Cover = coverImage;
            MarkDirty();
        }

        /// <summary>
        /// Sets the cover image using a Stream. May throw exceptions when reading the provided Stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public void SetCover(Stream stream)
        {
            if (stream == null)
                return;
            long streamLength = 0;
            try
            {
                streamLength = stream.Length;
            }
            catch { }
            MemoryStream ms;
            if (streamLength > 0)
            {
                ms = new MemoryStream((int)streamLength);
            }
            else
                ms = new MemoryStream();
            stream.CopyTo(ms);
            Cover = ms.ToArray();
            ms.Dispose();
        }

        public bool TryAdd(IPlaylistSong song)
        {
            if (song is BlisterPlaylistSong blisterSong)
                return TryAdd(blisterSong);
            else
                return TryAdd(new BlisterPlaylistSong(song));
        }

        public bool TryAdd(BlisterPlaylistSong song)
        {
            if (!AllowDuplicates && Beatmaps.FirstOrDefault(m => m.Equals(song)) != null)
                return false;
            Beatmaps.Add(song);
            return true;
        }

        public bool TryAdd(string songHash, string songName, string songKey, string mapper)
        {
            BlisterPlaylistSong existing = Beatmaps.FirstOrDefault(m => m.Hash == songHash || m.Key == songKey);
            if (existing != null)
                return false;
            Beatmaps.Add(new BlisterPlaylistSong()
            {
                Hash = songHash,
                Key = songKey,
                LevelAuthorName = mapper,
                DateAdded = DateTime.Now,
                Name = songName
            });
            return true;
        }

        public bool TryRemove(string songHashOrKey)
        {
            if (string.IsNullOrEmpty(songHashOrKey))
                return false;
            int numRemoved = Beatmaps.RemoveAll(m => songHashOrKey.Equals(m.Hash, StringComparison.OrdinalIgnoreCase) || songHashOrKey.Equals(m.Key, StringComparison.OrdinalIgnoreCase));
            return numRemoved > 0;
        }

        public bool TryRemove(IPlaylistSong song)
        {
            if (song == null)
                return false;
            bool songRemoved = TryRemove(song.Hash);
            songRemoved = TryRemove(song.Key) || songRemoved;
            return songRemoved;
        }

        public int RemoveAll(Func<BlisterPlaylistSong, bool> match)
        {
            int songsRemoved = Beatmaps.RemoveAll(m => match(m));
            if (songsRemoved > 0)
                MarkDirty();
            return songsRemoved;
        }

        public void RemoveDuplicates()
        {
            int previousCount = Beatmaps.Count;
            Beatmaps = Beatmaps.Distinct().ToList();
            if (Beatmaps.Count != previousCount)
                MarkDirty();
        }
        public void Clear()
        {
            if (Count > 0)
            {
                Beatmaps?.Clear();
                MarkDirty();
            }
        }

        public bool TryStore()
        {
            string backupName = null;
            if (File.Exists(FilePath))
            {
                backupName = FilePath + ".bak";
                File.Move(FilePath, backupName);
            }
            using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                BlisterHandler.SerializeStream(this, fs);
            }
            if (backupName != null)
                File.Delete(backupName);
            return true;
        }

        public bool TryStore(out Exception exception)
        {
            exception = null;
            try
            {
                return TryStore();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
