using System;
using System.Collections.Generic;
using System.Drawing;
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
            Directory.CreateDirectory(Paths.GetFullPath(ConfigDirectory, PathRoot.AssemblyDirectory));
        }

        public string BeatSyncConfigPath
        {
            get
            {
                return Config?.BeatSyncConfigPath?.Replace("%CONFIG%", ConfigDirectory, StringComparison.OrdinalIgnoreCase)
                    ?? Path.Combine(ConfigDirectory, "BeatSync.json");
            }
        }

        public string ConsoleConfigPath { get; protected set; }

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
            foreach (var l in Config.BeatSaberInstallLocations)
            {
                if (l.IsValid(out string? invalidReason))
                    songLocations.Add(l);
                else
                    Logger.log?.Warn($"Location '{l.BasePath}' is invalid: {(invalidReason ?? "Unknown reason.")}");
            }
            foreach (var l in Config.AlternateSongsPaths)
            {
                if (l.IsValid(out string? invalidReason))
                    songLocations.Add(l);
                else if(!string.IsNullOrEmpty(l.BasePath))
                    Logger.log?.Warn($"Location '{l.BasePath}' is invalid: {(invalidReason ?? "Unknown reason.")}");
            }
            return songLocations;
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
                if (ex.LinePosition > 0 && ex.LinePosition < line.Length)
                {
                    Logger.log.Log(line, BeatSyncLib.Logging.LogLevel.Warn, new ColoredSection[]
                    {
                            new ColoredSection(ex.LinePosition - 2, 3, ConsoleColor.Red),
                    });
                }
            }
            if (ex.Message.StartsWith(@"Bad JSON escape sequence: \"))
            {
                Logger.log.Warn($"If a setting uses the '\\' character (such as a path), make sure you use two of them ('C:\\\\Program Files\\\\Steam').");
            }
            Logger.log.Debug(ex);
        }

        public async Task<bool> InitializeConfigAsync()
        {
            Directory.CreateDirectory(Paths.GetFullPath(ConfigDirectory, PathRoot.AssemblyDirectory));
            ConsoleConfigPath = Path.Combine(ConfigDirectory, BeatSyncConsoleConfigName);
            string fullConsolePath = Paths.GetFullPath(ConsoleConfigPath, PathRoot.AssemblyDirectory);
            bool validConfig = true;
            try
            {
                if (File.Exists(fullConsolePath))
                {
                    Config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(fullConsolePath).ConfigureAwait(false));
                }
                else
                {
                    Logger.log.Info($"{ConsoleConfigPath} not found, creating a new one.");
                    Config = Config.GetDefaultConfig();
                }
            }
            catch (JsonReaderException ex)
            {
                WriteJsonException(ConsoleConfigPath, ex);
                Config = null;
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Invalid BeatSyncConsole.json file, using defaults: {ex.Message}");
                Logger.log.Debug(ex);
                Config = null;
            }
            if (Config == null)
            {
                string? response = LogManager.GetUserInput($"Would you like to replace the existing '{ConsoleConfigPath}' with a new one? (Y/N): ");
                if (response?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    Config = Config.GetDefaultConfig();
                }
                else
                    return false;
            }
            Paths.UseLocalTemp = !Config.UseSystemTemp;
            string fullBeatSyncConfigPath = Paths.GetFullPath(BeatSyncConfigPath, PathRoot.AssemblyDirectory);
            try
            {
                if (File.Exists(fullBeatSyncConfigPath))
                {
                    Logger.log.Info($"Using BeatSync config '{BeatSyncConfigPath}'.");
                    Config.BeatSyncConfig = JsonConvert.DeserializeObject<BeatSyncConfig>(await File.ReadAllTextAsync(fullBeatSyncConfigPath).ConfigureAwait(false));
                }
                else
                {
                    Logger.log.Info($"{BeatSyncConfigPath} not found, creating a new one.");
                    Config.BeatSyncConfig = new BeatSyncConfig(true);
                }
            }
            catch (JsonReaderException ex)
            {
                WriteJsonException(BeatSyncConfigPath, ex);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Invalid BeatSync.json file, using defaults: {ex.Message}");
                Logger.log.Debug(ex);
            }
            if (Config.BeatSyncConfig == null)
            {
                string? response = LogManager.GetUserInput($"Would you like to replace the existing '{BeatSyncConfigPath}' with a new one? (Y/N): ");
                if (response?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    Config.BeatSyncConfig = new BeatSyncConfig(true);
                }
                else
                    return false;
            }
            Config.FillDefaults();
            ISongLocation[] enabledPaths = GetValidEnabledLocations().ToArray();
            ISongLocation[] validPaths = GetValidLocations().ToArray();
            if (enabledPaths.Length == 0)
            {
                if (validPaths.Length == 0)
                {
#if !NOREGISTRY
                    string? response = LogManager.GetUserInput($"No song paths found in '{ConsoleConfigPath}', should I search for game installs? (Y/N): ");
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
                    Logger.log.Warn("No locations currently enabled.");
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
                            Logger.log.Warn($"'{selectionResponse[i]}' is invalid.");
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
                Logger.log.Warn($"No enabled custom songs paths found, please manually enter a target directory for your songs in '{ConsoleConfigPath}'.");
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
            var beastSaberConfig = Config.BeatSyncConfig.BeastSaber;
            if (validConfig && beastSaberConfig.Enabled
                && string.IsNullOrEmpty(beastSaberConfig.Username))
            {
                if (beastSaberConfig.Bookmarks.Enabled || beastSaberConfig.Follows.Enabled)
                {
                    string? username = LogManager.GetUserInput("You have BeastSaber feeds enabled that require your bsaber.com username, enter your username:");
                    if (username != null && username.Length > 0)
                    {
                        beastSaberConfig.Username = username;
                    }
                    else
                        Logger.log.Warn($"No BeastSaber username entered, BeastSaber feeds 'Bookmarks' and 'Follows' will be unavailable.");
                }
            }
            if (Config.ConfigChanged || Config.legacyValueChanged)
            {
                await StoreConsoleConfig().ConfigureAwait(false);
            }
            if (Config.BeatSyncConfig.ConfigChanged)
            {
                await StoreBeatSyncConfig().ConfigureAwait(false);
            }
            return validConfig;
        }

        public async Task StoreConsoleConfig()
        {
            try
            {
                string fullConsolePath = Paths.GetFullPath(ConsoleConfigPath, PathRoot.AssemblyDirectory);
                string backup = fullConsolePath + ".bak";
                if (File.Exists(fullConsolePath))
                    File.Copy(fullConsolePath, backup);
                await File.WriteAllTextAsync(fullConsolePath, JsonConvert.SerializeObject(Config, Formatting.Indented)).ConfigureAwait(false);
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            catch (Exception ex)
            {
                Logger.log.Error("Error updating config file.");
                Logger.log.Info(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if Config is null.</exception>
        public async Task StoreBeatSyncConfig()
        {
            if (Config == null || Config.BeatSyncConfig == null)
                throw new InvalidOperationException("Nothing to store, Config is null.");
            try
            {
                string fullBeatSyncConfigPath = Paths.GetFullPath(BeatSyncConfigPath, PathRoot.AssemblyDirectory);
                string backup = fullBeatSyncConfigPath + ".bak";
                if (File.Exists(fullBeatSyncConfigPath))
                    File.Copy(fullBeatSyncConfigPath, backup);
                await File.WriteAllTextAsync(fullBeatSyncConfigPath, JsonConvert.SerializeObject(Config.BeatSyncConfig, Formatting.Indented)).ConfigureAwait(false);
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            catch (Exception ex)
            {
                Logger.log.Error("Error updating config file.");
                Logger.log.Info(ex);
            }
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
            if (inBeatSyncConfigPath != null && File.Exists(inBeatSyncConfigPath))
                return inBeatSyncConfigPath;

            string inConfigDirPath = Path.GetFullPath(Path.Combine(ConfigDirectory, fileName));
            if (File.Exists(inConfigDirPath))
                return inConfigDirPath;
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);
            foreach (var location in songLocations.Where(l => l is BeatSaberInstallLocation))
            {
                string path = Path.Combine(location.FullBasePath, "UserData", fileName);
                if (File.Exists(path))
                    return path;
            }
            foreach (var location in songLocations.Where(l => l is CustomSongLocation))
            {
                string path = Path.Combine(location.FullBasePath, fileName);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

    }
}
