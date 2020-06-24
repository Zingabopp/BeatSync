using BeatSync.Configs;
using BeatSyncLib.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace BeatSync
{
    public class ConfigManager
    {
        internal const string BeatSyncModConfigName = "BeatSyncMod.json";
        internal const string BeatSyncConfigName = "BeatSync.json";
        internal const string FavoriteMappersName = "FavoriteMappers.ini";
        public readonly string ConfigDirectory = IPA.Utilities.UnityGame.UserDataPath;
        public BeatSyncModConfig? Config { get; private set; }
        public ConfigManager()
        {
            Directory.CreateDirectory(ConfigDirectory);
        }

        public string BeatSyncModConfigPath
        {
            get
            {
                return Path.Combine(ConfigDirectory, BeatSyncModConfigName);
            }
        }

        public string BeatSyncConfigPath
        {
            get
            {
                return Path.Combine(ConfigDirectory, BeatSyncConfigName);
            }
        }

        public string FavoriteMappersPath
        {
            get
            {
                return Path.Combine(ConfigDirectory, FavoriteMappersName);
            }
        }

        public static void WriteJsonException(string sourceFile, JsonReaderException ex)
        {
            Logger.log.Error($"Invalid JSON in {sourceFile} on line {ex.LineNumber} position {ex.LinePosition}.");
            string? line = null;
            try
            {
                int skip = Math.Max(ex.LineNumber - 1, 0);
                line = File.ReadLines(sourceFile).Skip(skip).Take(1).FirstOrDefault();
            }
            catch { }
            if (line != null)
            {
                Logger.log.Error(line);
            }
            Logger.log.Debug(ex);
        }

        public bool InitializeConfig()
        {
            Directory.CreateDirectory(ConfigDirectory);
            string modConfigPath = BeatSyncModConfigPath;
            bool validConfig = true;
            try
            {
                if (File.Exists(modConfigPath))
                {
                    Config = JsonConvert.DeserializeObject<BeatSyncModConfig>(File.ReadAllText(modConfigPath));
                }
                else
                {
                    Logger.log.Info($"{modConfigPath} not found, creating a new one.");
                    Config = BeatSyncModConfig.GetDefaultConfig();
                }
            }
            catch (JsonReaderException ex)
            {
                WriteJsonException(modConfigPath, ex);
                Config = null;
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Invalid {BeatSyncModConfigName} file, using defaults: {ex.Message}");
                Logger.log.Debug(ex);
                Config = null;
            }
            if (Config == null)
            {
                Config = BeatSyncModConfig.GetDefaultConfig();
            }
            string beatSyncConfigPath = BeatSyncConfigPath;

            try
            {
                if (File.Exists(beatSyncConfigPath))
                {
                    Logger.log.Debug($"Using BeatSync config '{beatSyncConfigPath}'.");
                    Config.BeatSyncConfig = JsonConvert.DeserializeObject<BeatSyncConfig>(File.ReadAllText(beatSyncConfigPath));
                }
                else
                {
                    Logger.log.Info($"{beatSyncConfigPath} not found, creating a new one.");
                }
            }
            catch (JsonReaderException ex)
            {
                WriteJsonException(beatSyncConfigPath, ex);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Invalid BeatSync.json file, using defaults: {ex.Message}");
                Logger.log.Debug(ex);
            }
            if (Config.BeatSyncConfig == null)
            {
                Config.BeatSyncConfig = new BeatSyncConfig(true);
            }
            Config.FillDefaults();

            string? favoriteMappersPath = FavoriteMappersPath;
            if (validConfig && favoriteMappersPath != null)
            {
                FavoriteMappers favoriteMappers = new FavoriteMappers(favoriteMappersPath);
                Logger.log.Info($"Getting FavoriteMappers from '{favoriteMappersPath.Replace(Directory.GetCurrentDirectory(), ".")}'.");
                List<string> mappers = favoriteMappers.ReadFromFile();
                if (mappers.Count > 0)
                    Config.BeatSyncConfig.BeatSaver.FavoriteMappers.Mappers = mappers.ToArray();
            }
            var beastSaberConfig = Config.BeatSyncConfig.BeastSaber;

            SaveConfig();
            return validConfig;
        }

        public bool SaveConfig(bool force = false)
        {
            string modConfigPath = BeatSyncModConfigPath;
            string beatSyncConfigPath = BeatSyncConfigPath;
            if (Config == null)
                return false;
            bool failed = false;
            if (Config.ConfigChanged || force)
            {
                try
                {
                    string backup = modConfigPath + ".bak";
                    if (File.Exists(modConfigPath))
                        File.Copy(modConfigPath, backup);
                    File.WriteAllText(modConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
                    if (File.Exists(backup))
                        File.Delete(backup);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Error updating config file.");
                    Logger.log.Info(ex);
                    failed = true;
                }
            }
            if (Config.BeatSyncConfig.ConfigChanged || force)
            {
                try
                {
                    string backup = beatSyncConfigPath + ".bak";
                    if (File.Exists(beatSyncConfigPath))
                        File.Copy(beatSyncConfigPath, backup);
                    File.WriteAllText(beatSyncConfigPath, JsonConvert.SerializeObject(Config.BeatSyncConfig, Formatting.Indented));
                    if (File.Exists(backup))
                        File.Delete(backup);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Error updating config file.");
                    Logger.log.Info(ex);
                    failed = true;
                }
            }
            return failed;
        }
    }
}
