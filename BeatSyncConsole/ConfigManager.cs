using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Configs;
using Newtonsoft.Json;

namespace BeatSyncConsole
{
    public class ConfigManager
    {
        internal const string BeatSyncConsoleConfigName = "BeatSyncConsole.json";
        //internal const string BeatSyncConfigName = "BeatSync.json";
        public readonly string ConfigDirectory;
        public Config? Config { get; private set; }
        public ConfigManager(string configDirectory)
        {
            if (string.IsNullOrEmpty(configDirectory))
                throw new ArgumentNullException(nameof(configDirectory));
            ConfigDirectory = configDirectory;
            Directory.CreateDirectory(ConfigDirectory);
        }

        public string BeatSyncConfigPath
        {
            get
            {
                return Config?.BeatSyncConfigPath?.Replace("%CONFIG%", ConfigDirectory, StringComparison.OrdinalIgnoreCase) 
                    ?? Path.Combine(ConfigDirectory, "BeatSync.json");
            }
        }

        public IEnumerable<ISongLocation> GetValidEnabledLocations()
        {
            if (Config == null)
                throw new InvalidOperationException("Config is null.");
            List<ISongLocation> songLocations = new List<ISongLocation>();
            songLocations.AddRange(Config.BeatSaberInstallLocations.Where(l => l.Enabled && l.IsValid()));
            songLocations.AddRange(Config.AlternateSongsPaths.Where(l => l.Enabled && l.IsValid()));
            return songLocations;
        }

        public IEnumerable<ISongLocation> GetValidLocations()
        {
            if (Config == null)
                throw new InvalidOperationException("Config is null.");
            List<ISongLocation> songLocations = new List<ISongLocation>();
            songLocations.AddRange(Config.BeatSaberInstallLocations.Where(l => l.IsValid()));
            songLocations.AddRange(Config.AlternateSongsPaths.Where(l => l.IsValid()));
            return songLocations;
        }

        public async Task<bool> InitializeConfigAsync()
        {
            Directory.CreateDirectory(ConfigDirectory);
            string consoleConfigPath = Path.Combine(ConfigDirectory, BeatSyncConsoleConfigName);
            bool validConfig = true;
            try
            {
                if (File.Exists(consoleConfigPath))
                {
                    Config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(consoleConfigPath).ConfigureAwait(false));

                }
                else
                {
                    Logger.log.Info($"{consoleConfigPath} not found, creating a new one.");
                    Config = Config.GetDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.log.Info($"Invalid BeatSyncConsole.json file, using defaults: {ex.Message}");
                Config = Config.GetDefaultConfig();
            }
            string beatSyncConfigPath = Config.BeatSyncConfigPath.Replace("%CONFIG%", ConfigDirectory, StringComparison.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(beatSyncConfigPath))
                {
                    Logger.log.Info($"Using BeatSync config '{beatSyncConfigPath}'.");
                    Config.BeatSyncConfig = JsonConvert.DeserializeObject<BeatSyncConfig>(await File.ReadAllTextAsync(beatSyncConfigPath).ConfigureAwait(false));
                }
                else
                {
                    Logger.log.Info($"{beatSyncConfigPath} not found, creating a new one.");
                }
            }
            catch (Exception ex)
            {
                Logger.log.Info($"Invalid BeatSync.json file, using defaults: {ex.Message}");
                Logger.log.Debug(ex);
            }
            if (Config.BeatSyncConfig == null)
            {
                Config.BeatSyncConfig = new BeatSyncConfig(true);
            }
            Config.FillDefaults();
            ISongLocation[] enabledPaths = GetValidEnabledLocations().ToArray();
            ISongLocation[] validPaths = GetValidLocations().ToArray();
            if (enabledPaths.Length == 0)
            {
                if (validPaths.Length == 0)
                {
#if !NOREGISTRY
                    string? response = LogManager.GetUserInput("No song paths found in BeatSync.json, should I search for game installs? (Y/N): ");
                    if (response == "Y" || response == "y")
                    {
                        BeatSaberInstall[] gameInstalls = BeatSaberTools.GetBeatSaberPathsFromRegistry();
                        //BeatSaberInstall[] gameInstalls = Array.Empty<BeatSaberInstall>();
                        if (gameInstalls.Length > 0)
                        {
                            Config.BeatSaberInstallLocations.Clear();
                            for (int i = 0; i < gameInstalls.Length; i++)
                            {
                                Logger.log.Info($"  {gameInstalls[i]}");
                                BeatSaberInstallLocation newLocation = gameInstalls[i].ToSongLocation();
                                newLocation.Enabled = false;
                                Config.BeatSaberInstallLocations.Add(newLocation);
                            }
                            if (gameInstalls.Length == 1)
                            {
                                Logger.log.Info($"Found 1 game install, enabling for BeatSyncConsole: {gameInstalls[0]}");
                                Config.BeatSaberInstallLocations[0].Enabled = true; ;
                            }
                            else
                            {
                                Logger.log.Info($"Found {gameInstalls.Length} game installs:");
                            }
                            Config.SetConfigChanged(true, nameof(Config.BeatSaberInstallLocations));
                        }
                    }
#endif
                }
                enabledPaths = GetValidEnabledLocations().ToArray();
                validPaths = GetValidLocations().ToArray();
                if (enabledPaths.Length == 0 && validPaths.Length > 0)
                {
                    Logger.log.Info("No locations currently enabled.");
                    for (int i = 0; i < validPaths.Length; i++)
                    {
                        Logger.log.Info($"  {i}: {validPaths[i]}");
                    }
                    string? response = LogManager.GetUserInput($"Enter the numbers of the locations you wish to enable, separated by commas.")
                        ?? string.Empty;
                    string[] selectionResponse = response.Split(',');
                    int[] selectionInts = selectionResponse.Select(r =>
                    {
                        if (int.TryParse(r.Trim(), out int parsed))
                        {
                            return parsed;
                        }
                        return -1;
                    }).ToArray();
                    for (int i = 0; i < selectionInts.Length; i++)
                    {
                        int current = selectionInts[i];
                        if (current > -1 && current < validPaths.Length)
                        {
                            validPaths[current].Enabled = true;
                            Logger.log.Info($"Enabling {validPaths[current]}.");
                            Config.SetConfigChanged(true, nameof(Config.AlternateSongsPaths));
                        }
                        else
                            Logger.log.Info($"'{selectionResponse[i]}' is invalid.");
                    }
                }
            }
            enabledPaths = GetValidEnabledLocations().ToArray();
            if (enabledPaths.Length > 0)
            {
                Logger.log.Info("Using the following targets:");
                foreach (ISongLocation enabledLocation in enabledPaths)
                {
                    Logger.log.Info($"  {enabledLocation}");
                }
            }
            else
            {
                Logger.log.Info("No enabled custom songs paths found, please manually enter a target directory for your songs in config.json.");
                validConfig = false;
            }
            string? favoriteMappersPath = GetFavoriteMappersLocation(enabledPaths);
            if (validConfig && favoriteMappersPath != null)
            {
                FavoriteMappers favoriteMappers = new FavoriteMappers(favoriteMappersPath);
                Logger.log.Info($"Getting FavoriteMappers from '{favoriteMappersPath.Replace(Directory.GetCurrentDirectory(), ".")}'.");
                List<string> mappers = favoriteMappers.ReadFromFile();
                if (mappers.Count > 0)
                    Config.BeatSyncConfig.BeatSaver.FavoriteMappers.Mappers = mappers.ToArray();
            }
            if (Config.ConfigChanged || Config.legacyValueChanged)
            {
                try
                {
                    string backup = consoleConfigPath + ".bak";
                    if (File.Exists(consoleConfigPath))
                        File.Copy(consoleConfigPath, backup);
                    await File.WriteAllTextAsync(consoleConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented)).ConfigureAwait(false);
                    if (File.Exists(backup))
                        File.Delete(backup);
                }
                catch (Exception ex)
                {
                    Logger.log.Info("Error updating config file.");
                    Logger.log.Info(ex);
                }
            }
            if (Config.BeatSyncConfig.ConfigChanged)
            {
                try
                {
                    string backup = beatSyncConfigPath + ".bak";
                    if (File.Exists(beatSyncConfigPath))
                        File.Copy(beatSyncConfigPath, backup);
                    await File.WriteAllTextAsync(beatSyncConfigPath, JsonConvert.SerializeObject(Config.BeatSyncConfig, Formatting.Indented)).ConfigureAwait(false);
                    if (File.Exists(backup))
                        File.Delete(backup);
                }
                catch (Exception ex)
                {
                    Logger.log.Info("Error updating config file.");
                    Logger.log.Info(ex);
                }
            }
            return validConfig;
        }

        public string? GetFavoriteMappersLocation(IEnumerable<ISongLocation> songLocations)
        {
            string fileName = "FavoriteMappers.ini";
            string? inBeatSyncConfigPath = null;
            try
            {
                string? beatSyncConfigDirectory = Path.GetDirectoryName(BeatSyncConfigPath);
                if (beatSyncConfigDirectory != null)
                    inBeatSyncConfigPath = Path.GetFullPath(Path.Combine(beatSyncConfigDirectory, fileName));
            }
            catch { }
            if(inBeatSyncConfigPath != null && File.Exists(inBeatSyncConfigPath))
                return inBeatSyncConfigPath;

            string inConfigDirPath = Path.GetFullPath(Path.Combine(ConfigDirectory, fileName));
            if (File.Exists(inConfigDirPath))
                return inConfigDirPath;
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);
            foreach (var location in songLocations.Where(l => l is BeatSaberInstallLocation))
            {
                string path = Path.Combine(location.BasePath, "UserData", fileName);
                if (File.Exists(path))
                    return path;
            }
            foreach (var location in songLocations.Where(l => l is CustomSongLocation))
            {
                string path = Path.Combine(location.BasePath, fileName);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

    }
}
