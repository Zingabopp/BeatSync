using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BeatSync.Configs
{
    public class FavoriteMappers
    {
        private static readonly string DefaultFilePath = Path.GetFullPath(Path.Combine("UserData", "FavoriteMappers.ini"));
        public string FilePath { get; private set; }
        private List<string> _mappers;
        public List<string> Mappers
        {
            get
            {
                if (_mappers == null)
                    _mappers = new List<string>();
                return _mappers;
            }
        }

        public FavoriteMappers(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultFilePath;
            FilePath = filePath;
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
                Logger.log?.Debug($"Couldn't find {FilePath}, skipping");
                return mapperList;
            }
            try
            {
                using (var sr = File.OpenText(FilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        line = line?.Trim();
                        if (!string.IsNullOrEmpty(line))
                            mapperList.Add(line);
                    }
                }
                Logger.log?.Info($"Loaded {mapperList.Count} mappers from FavoriteMappers.ini");
            }
            catch (Exception ex)
            {
                Logger.log?.Warn(ex);
            }
            return mapperList;
        }
    }
}
