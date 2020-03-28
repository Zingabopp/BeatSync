/*
MIT License

Copyright (c) 2019 Jack Baron DEV

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Linq;

namespace BeatSyncLib.Playlists.Blister
{
    /// <summary>
    /// Tools to serialize and deserialize Blister playlists.
    /// From: https://github.com/lolPants/Blister/blob/master/Blister/PlaylistLib.cs
    /// </summary>
    public class BlisterHandler : IPlaylistHandler<BlisterPlaylist>
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        #region Static
        private static readonly string MagicNumberString = "Blist.v2";
        /// <summary>
        /// Blister format Magic Number
        /// <br />
        /// UTF-8 Encoded "Blist.v2"
        /// </summary>
        public static readonly byte[] MagicNumber = Encoding.UTF8.GetBytes(MagicNumberString);
        private static Stream ReadMagicNumber(Stream stream)
        {
            byte[] magicBytes = new byte[MagicNumber.Length];

            for (int i = 0; i < magicBytes.Length; i++)
            {
                magicBytes[i] = (byte)stream.ReadByte();
            }

            bool hasMagicNumber = magicBytes.SequenceEqual(MagicNumber);
            if (!hasMagicNumber)
            {
                throw new InvalidMagicNumberException();
            }

            return stream;
        }

        /// <summary>
        /// Serialize a playlist struct to a Memory Stream
        /// </summary>
        /// <param name="playlist">Playlist struct</param>
        /// <param name="stream">Stream to write to</param>
        /// <returns></returns>
        public static void SerializeStream(BlisterPlaylist playlist, Stream stream)
        {
            stream.Write(MagicNumber, 0, MagicNumber.Length);

            using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress))
            using (BsonDataWriter writer = new BsonDataWriter(gzip))
            {
                serializer.Serialize(writer, playlist);
            }
        }


        /// <summary>
        /// Deserialize a BSON byte stream to a <see cref="BlisterPlaylist"/>.
        /// </summary>
        /// <param name="stream">Byte Stream</param>
        /// <returns></returns>
        /// <exception cref="InvalidMagicNumberException"></exception>
        public static BlisterPlaylist DeserializeFromStream(Stream stream)
        {
            using (Stream magic = ReadMagicNumber(stream))
            using (GZipStream gzip = new GZipStream(magic, CompressionMode.Decompress))
            using (BsonDataReader reader = new BsonDataReader(gzip))
            {
                return serializer.Deserialize<BlisterPlaylist>(reader);
            }
        }


        /// <summary>
        /// Deserialize a BSON byte stream and populates the target <see cref="BlisterPlaylist"/> with the data.
        /// </summary>
        /// <param name="stream">Byte Stream</param>
        /// <returns></returns>
        /// <exception cref="InvalidMagicNumberException"></exception>
        public static void PopulateFromStream(Stream stream, BlisterPlaylist target)
        {
            using (Stream magic = ReadMagicNumber(stream))
            using (GZipStream gzip = new GZipStream(magic, CompressionMode.Decompress))
            using (BsonDataReader reader = new BsonDataReader(gzip))
            {
                serializer.Populate(reader, target);
            }
        }

        #endregion
        public string[] GetSupportedExtensions()
        {
            return new string[] { "blist" };
        }

        public Type HandledType { get; } = typeof(BlisterPlaylist);

        /// <summary>
        /// Deserialize BSON bytes to a Playlist struct
        /// </summary>
        /// <param name="bytes">BSON bytes</param>
        /// <returns></returns>
        /// <exception cref="InvalidMagicNumberException"></exception>
        public BlisterPlaylist Deserialize(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return DeserializeFromStream(ms);
            }
        }
        public static BlisterPlaylist Deserialize(Stream stream) => DeserializeFromStream(stream);
        public BlisterPlaylist Deserialize(string path)
        {
            using (FileStream stream = File.OpenRead(path))
                return DeserializeFromStream(stream);
        }

        public void Populate(Stream stream, BlisterPlaylist target) => PopulateFromStream(stream, target);

        /// <summary>
        /// Serialize a playlist struct to a byte array
        /// </summary>
        /// <param name="playlist">Playlist struct</param>
        /// <returns></returns>
        public byte[] Serialize(BlisterPlaylist playlist)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                SerializeToStream(playlist, ms);
                return ms.ToArray();
            }
        }
        public void SerializeToStream(BlisterPlaylist playlist, Stream stream) => SerializeStream(playlist, stream);
        public void SerializeToFile(BlisterPlaylist playlist, string path)
        {
            using (FileStream stream = File.OpenWrite(path))
                SerializeToStream(playlist, stream);
        }




        #region IPlaylistHandler
        void IPlaylistHandler.SerializeToStream(IPlaylist playlist, Stream stream)
        {
            BlisterPlaylist legacyPlaylist = (playlist as BlisterPlaylist) ?? throw new ArgumentException($"{playlist.GetType().Name} is not a supported Type for {nameof(BlisterHandler)}");
            SerializeToStream(legacyPlaylist, stream);
        }

        void IPlaylistHandler.SerializeToFile(IPlaylist playlist, string path)
        {
            BlisterPlaylist legacyPlaylist = (playlist as BlisterPlaylist) ?? throw new ArgumentException($"{playlist.GetType().Name} is not a supported Type for {nameof(BlisterHandler)}");
            SerializeToFile(legacyPlaylist, path);
        }

        void IPlaylistHandler.Populate(Stream stream, IPlaylist target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), $"{nameof(target)} cannot be null.");
            BlisterPlaylist legacyPlaylist = (target as BlisterPlaylist) ?? throw new ArgumentException($"{target.GetType().Name} is not a supported Type for {nameof(BlisterHandler)}");
            PopulateFromStream(stream, legacyPlaylist);
        }

        IPlaylist IPlaylistHandler.Deserialize(string path)
        {
            return Deserialize(path);
        }

        IPlaylist IPlaylistHandler.Deserialize(Stream stream)
        {
            return DeserializeFromStream(stream);
        }
        #endregion
    }

    /// <summary>
    /// Raised when the file being deserialized does not contain a correct magic number
    /// </summary>
    public class InvalidMagicNumberException : InvalidOperationException
    {
        public InvalidMagicNumberException()
        {
        }
        public InvalidMagicNumberException(string message) : base(message)
        {
        }

        public InvalidMagicNumberException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
