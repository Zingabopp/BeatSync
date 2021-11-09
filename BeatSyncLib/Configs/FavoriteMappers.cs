using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SongFeedReaders.Logging;

namespace BeatSyncLib.Configs
{
    public class FavoriteMappers
    {
        private static readonly string DefaultFilePath = Path.GetFullPath(Path.Combine("UserData", "FavoriteMappers.ini"));
        protected readonly ILogger? Logger;
        public string FilePath { get; private set; }
        private List<string>? _mappers;
        public List<string> Mappers
        {
            get
            {
                if (_mappers == null)
                    _mappers = new List<string>();
                return _mappers;
            }
        }

        public FavoriteMappers(string? filePath = null, ILogFactory logFactory = null)
        {
            if (filePath == null || filePath == string.Empty)
                filePath = DefaultFilePath;
            FilePath = filePath;
            Logger = logFactory?.GetLogger(GetType().Name);
        }

        public void Initialize()
        {
            _mappers = ReadFromFile();
        }

        public List<string> ReadFromFile()
        {
            var mapperList = new List<string>();
            if (!File.Exists(FilePath))
            {
                Logger?.Debug($"Couldn't find {FilePath}, skipping");
                return mapperList;
            }
            try
            {
                using (var sr = File.OpenText(FilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                                mapperList.Add(line);
                        }
                    }
                }
                Logger?.Info($"Loaded {mapperList.Count} mappers from FavoriteMappers.ini");
            }
            catch (Exception ex)
            {
                Logger?.Warning(ex);
            }
            return mapperList;
        }
    }
}
