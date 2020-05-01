using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BeatSyncPlaylists
{
    public interface IPlaylistHandler
    {
        string[] GetSupportedExtensions();
        Type HandledType { get; }
        IPlaylist Deserialize(string path);
        IPlaylist Deserialize(Stream stream);
        void SerializeToStream(IPlaylist playlist, Stream stream);
        void SerializeToFile(IPlaylist playlist, string path);
        void Populate(Stream stream, IPlaylist target);
    }

    public interface IPlaylistHandler<T> 
        : IPlaylistHandler where T : IPlaylist
    {
        void SerializeToStream(T playlist, Stream stream);
        void SerializeToFile(T playlist, string path);
        void Populate(Stream stream, T target);
    }
}
