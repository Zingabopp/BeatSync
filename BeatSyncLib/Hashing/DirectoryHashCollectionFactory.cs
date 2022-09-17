using BeatSaber.SongHashing;
using SongFeedReaders.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Hashing
{
    public class DirectoryHashCollectionFactory : ISongHashCollectionFactory
    {
        private readonly ConcurrentDictionary<string, ISongHashCollection> _dictionary 
            = new ConcurrentDictionary<string, ISongHashCollection>();

        private readonly IBeatmapHasher BeatmapHasher;
        private readonly ILogFactory? LogFactory;
        public DirectoryHashCollectionFactory(IBeatmapHasher beatmapHasher, ILogFactory? logFactory)
        {
            BeatmapHasher = beatmapHasher ?? throw new ArgumentNullException(nameof(beatmapHasher));
            LogFactory = logFactory;
        }

        public ISongHashCollection GetCollection(string path)
        {
            return _dictionary.GetOrAdd(path, s => new DirectoryHashCollection(s, BeatmapHasher, LogFactory));
        }
    }
}
