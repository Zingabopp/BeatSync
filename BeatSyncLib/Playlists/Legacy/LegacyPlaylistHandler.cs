using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeatSyncLib.Utilities;

namespace BeatSyncLib.Playlists.Legacy
{
    public class LegacyPlaylistHandler : IPlaylistHandler<LegacyPlaylist>
    {
        private static readonly JsonSerializer jsonSerializer = new JsonSerializer() { Formatting = Formatting.Indented };
        public string[] GetSupportedExtensions()
        {
            return new string[] { "bplist", "json" };
        }

        public Type HandledType { get; } = typeof(LegacyPlaylist);

        public void Populate(Stream stream, LegacyPlaylist target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), $"{nameof(target)} cannot be null for {nameof(Populate)}.");
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), $"{nameof(stream)} cannot be null for {nameof(Populate)}.");
            string str = null;
            using (StreamReader sr = new StreamReader(stream))
                str = sr.ReadToEnd();
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("Input stream gave an empty string.", nameof(stream));
            JsonConvert.PopulateObject(str, target);
        }
        public LegacyPlaylist Deserialize(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                return (LegacyPlaylist)jsonSerializer.Deserialize(sr, typeof(LegacyPlaylist));
            }
        }

        public LegacyPlaylist Deserialize(string path)
        {
            using (FileStream stream = File.OpenRead(path))
                return Deserialize(stream);
        }

        public void SerializeToStream(LegacyPlaylist playlist, Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
                jsonSerializer.Serialize(sw, playlist);
            }
        }

        public void SerializeToFile(LegacyPlaylist playlist, string path)
        {
            using (FileStream stream = File.OpenWrite(path))
                SerializeToStream(playlist, stream);
        }

        void IPlaylistHandler.SerializeToStream(IPlaylist playlist, Stream stream)
        {
            LegacyPlaylist legacyPlaylist = (playlist as LegacyPlaylist) ?? throw new ArgumentException($"{playlist.GetType().Name} is not a supported Type for {nameof(LegacyPlaylistHandler)}");
            SerializeToStream(legacyPlaylist, stream);
        }

        void IPlaylistHandler.SerializeToFile(IPlaylist playlist, string path)
        {
            LegacyPlaylist legacyPlaylist = (playlist as LegacyPlaylist) ?? throw new ArgumentException($"{playlist.GetType().Name} is not a supported Type for {nameof(LegacyPlaylistHandler)}");
            SerializeToFile(legacyPlaylist, path);
        }

        void IPlaylistHandler.Populate(Stream stream, IPlaylist target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), $"{nameof(target)} cannot be null.");
            LegacyPlaylist legacyPlaylist = (target as LegacyPlaylist) ?? throw new ArgumentException($"{target.GetType().Name} is not a supported Type for {nameof(LegacyPlaylistHandler)}");
            Populate(stream, legacyPlaylist);
        }

        IPlaylist IPlaylistHandler.Deserialize(string path)
        {
            return Deserialize(path);
        }

        IPlaylist IPlaylistHandler.Deserialize(Stream stream)
        {
            return Deserialize(stream);
        }
    }
}
