using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncConsole.Configs;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Configs;
using Newtonsoft.Json;

namespace BeatSyncConsole
{
    public class ConfigManager
    {
        internal const string BeatSyncConsoleConfigPath = "BeatSyncConsole.json";
        internal const string BeatSyncConfigPath = "BeatSync.json";
        public readonly string ConfigDirectory;
        public Config Config { get; private set; }
        public ConfigManager(string configDirectory)
        {
            if (string.IsNullOrEmpty(configDirectory))
                throw new ArgumentNullException(nameof(configDirectory));
            ConfigDirectory = configDirectory;
            Directory.CreateDirectory(ConfigDirectory);
        }

        public async Task<bool> InitializeConfigAsync()
        {
            Directory.CreateDirectory(ConfigDirectory);
            string consoleConfigPath = Path.Combine(ConfigDirectory, BeatSyncConsoleConfigPath);
            string beatSyncConfigPath = Path.Combine(ConfigDirectory, BeatSyncConfigPath);
            bool validConfig = true;
            try
            {
                if (File.Exists(consoleConfigPath))
                {
                    Config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(consoleConfigPath).ConfigureAwait(false));

                }
                else
                {
                    Console.WriteLine($"{consoleConfigPath} not found, creating a new one.");
                    Config = Config.GetDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid BeatSyncConsole.json file, using defaults: {ex.Message}");
                Config = Config.GetDefaultConfig();
            }
            try
            {
                if (File.Exists(beatSyncConfigPath))
                {
                    Config.BeatSyncConfig = JsonConvert.DeserializeObject<BeatSyncConfig>(await File.ReadAllTextAsync(beatSyncConfigPath).ConfigureAwait(false));

                }
                else
                {
                    Console.WriteLine($"{beatSyncConfigPath} not found, creating a new one.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid BeatSync.json file, using defaults: {ex.Message}");
            }
            if (Config.BeatSyncConfig == null)
            {
                Config.BeatSyncConfig = new BeatSyncConfig(true);
            }
            Config.FillDefaults();
            if (!Config.CustomSongsPaths.Any(p => p.Enabled))
            {
                if (Config.CustomSongsPaths.Count == 0
                    || (Config.CustomSongsPaths.Count == 1
                         && string.IsNullOrEmpty(Config.CustomSongsPaths[0].BasePath)))
                {
                    Console.WriteLine("No song paths found in BeatSync.json, should I search for game installs? (Y/N): ");
                    string response = Console.ReadLine();
                    if (response == "Y" || response == "y")
                    {
                        Utilities.BeatSaberInstall[] gameInstalls = BeatSaberTools.GetBeatSaberPathsFromRegistry();
                        if (gameInstalls.Length > 0)
                        {
                            Config.BeatSaberInstallLocations.Clear();
                            for (int i = 0; i < gameInstalls.Length; i++)
                            {
                                Console.WriteLine($"  {i}: {gameInstalls[i]}");
                                BeatSaberInstallLocation newLocation = gameInstalls[i].ToSongLocation();
                                newLocation.Enabled = false;
                                Config.BeatSaberInstallLocations.Add(newLocation);
                            }
                            if (gameInstalls.Length == 1)
                            {
                                Console.WriteLine($"Found 1 game install, enabling for BeatSyncConsole: {gameInstalls[0]}");
                                Config.BeatSaberInstallLocations[0].Enabled = true; ;
                            }
                            else
                            {
                                Console.WriteLine($"Found {gameInstalls.Length} game installs:");
                            }
                            Config.SetConfigChanged(true, nameof(Config.BeatSaberInstallLocations));
                        }
                    }
                }
                if (Config.CustomSongsPaths.Count > 0
                    && Config.CustomSongsPaths.Where(p => !p.Enabled).Count() == Config.CustomSongsPaths.Count
                    && !((Config.CustomSongsPaths.Count == 1
                         && string.IsNullOrEmpty(Config.CustomSongsPaths[0].BasePath))))
                {
                    Console.WriteLine("No locations currently enabled.");
                    for (int i = 0; i < Config.CustomSongsPaths.Count; i++)
                    {
                        Console.WriteLine($"  {i}: {Config.CustomSongsPaths[i]}");
                    }
                    Console.WriteLine($"Enter the numbers of the installs you wish to enable, separated by commas.");
                    string response = Console.ReadLine();
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
                        if (current > -1 && current < Config.CustomSongsPaths.Count)
                        {
                            Config.CustomSongsPaths[current].Enabled = true;
                            Console.WriteLine($"Enabling {Config.CustomSongsPaths[current]}.");
                            Config.SetConfigChanged(true, nameof(Config.CustomSongsPaths));
                        }
                        else
                            Console.WriteLine($"'{selectionResponse[i]}' is invalid.");
                    }
                }
            }
            if (Config.CustomSongsPaths.Any(p => p.Enabled && !string.IsNullOrEmpty(p.BasePath)))
            {
                Console.WriteLine("Using the following targets:");
                foreach (CustomSongLocation enabledLocation in Config.CustomSongsPaths.Where(p => p.Enabled))
                {
                    Console.WriteLine($"  {enabledLocation}");
                }
            }
            else
            {
                Console.WriteLine("No enabled custom songs paths found, please manually enter a target directory for your songs in config.json.");
                if (Config.CustomSongsPaths.Count == 0)
                {
                    Config.CustomSongsPaths.Add(CustomSongLocation.CreateEmptyLocation());
                    Config.SetConfigChanged(true, nameof(Config.CustomSongsPaths));
                }
                validConfig = false;
            }
            string? favoriteMappersPath = GetFavoriteMappersLocation(Config.CustomSongsPaths);
            if (favoriteMappersPath != null)
            {
                FavoriteMappers favoriteMappers = new FavoriteMappers(favoriteMappersPath);
                List<string> mappers = favoriteMappers.ReadFromFile();
                if (mappers.Count > 0)
                    Config.BeatSyncConfig.BeatSaver.FavoriteMappers.Mappers = mappers.ToArray();
            }
            if (Config.ConfigChanged)
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
                    Console.WriteLine("Error updating config file.");
                    Console.WriteLine(ex);
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
                    Console.WriteLine("Error updating config file.");
                    Console.WriteLine(ex);
                }
            }
            return validConfig;
        }

        public string? GetFavoriteMappersLocation(IEnumerable<ISongLocation> songLocations)
        {
            string fileName = "FavoriteMappers.ini";
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
