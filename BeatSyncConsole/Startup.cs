using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using BeatSyncConsole.CommandParser;
using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Downloader;
using BeatSyncLib.Utilities;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SongFeedReaders.Feeds;
using SongFeedReaders.Feeds.BeastSaber;
using SongFeedReaders.Feeds.BeatSaver;
using SongFeedReaders.Feeds.ScoreSaber;
using SongFeedReaders.Logging;
using SongFeedReaders.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using BeatSyncLib.History;
using BeatSyncLib.Hashing;
using BeatSyncLib.Downloader.Targets;
using static System.Net.WebRequestMethods;
using BeatSaber.SongHashing;

namespace BeatSyncConsole
{
    public class Startup
    {
        private const string ReleaseUrl = @"https://github.com/Zingabopp/BeatSync/releases";
        protected readonly IServiceCollection services;
        protected readonly string VersionString;
        protected ILogger Logger;
        protected BeatSyncLoggerSettings LoggerSettings = new BeatSyncLoggerSettings()
        {
            EnableTimeStamp = true,
            LogLevel = LogLevel.Info,
            ModuleName = "BeatSyncConsole",
            ShortSource = true,
            ShowModule = true
        };

        public Startup()
        {
            services = new ServiceCollection();
            AssemblyName ver = Assembly.GetExecutingAssembly().GetName();
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
#if BETA
            version += "-Beta";
#endif
            VersionString = version;
        }

        private ConsoleLogWriter SetupLogging(string? logDirectory)
        {
            ConsoleLogWriter consoleWriter = new ConsoleLogWriter
            {
                LogLevel = LogLevel.Info
            };
            LogManager.AddLogWriter(consoleWriter);
            Logger = new BeatSyncLogger(LoggerSettings);
            Program.Logger = Logger;
            try
            {
                if (logDirectory == null)
                    logDirectory = Paths.LogDirectory;
                string logFilePath = Path.Combine(logDirectory, "log.txt");
                Directory.CreateDirectory(logDirectory);
                LogManager.AddLogWriter(new FileLogWriter(logFilePath));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating FileLogWriter: {ex.Message}");
            }
            return consoleWriter;
        }
        private ILogFactory? LogFactory;
        private FileIO? FileIO;
        public void ConfigureServices(IServiceCollection services, Config config)
        {
            LoggerSettings.LogLevel = config.ConsoleLogLevel;
            LogFactory = new BeatSyncLoggerFactory(LoggerSettings);
            IWebClient webClient = new HttpClientWrapper($"BeatSyncConsole/{VersionString} ({RuntimeInformation.OSDescription}){VersionInfo.Description}");
            FileIO = new FileIO(webClient, LogFactory);
            services.AddSingleton(webClient);
            services.AddSingleton(LogFactory);
            services.AddSingleton(FileIO);
            services.AddSingleton<ISongDownloader, SongDownloader>();
            // services.AddSingleton<IPauseManager, PauseManager>();
            ConfigureFeeds(services, config);
        }

        public void ConfigureFeeds(IServiceCollection services, Config config)
        {
            services.AddSingleton<IBeatSaverPageHandler, BeatSaverPageHandler>();
            services.AddSingleton<IBeastSaberPageHandler, BeastSaberPageHandler>();
            services.AddSingleton<IScoreSaberPageHandler, ScoreSaberPageHandler>();
            foreach (var pair in FeedFactoryBase.GetAttributedFeeds(typeof(IFeedSettings).Assembly))
            {
                // Register all attributed feeds
                services.AddTransient(pair.Value);
            }
            services.AddSingleton<IFeedFactory, FeedFactory>();
        }


        public async Task ConfigureTargetsAsync(IServiceCollection services, Config config)
        {
            List<ISongLocation> songLocations = new List<ISongLocation>();
            FileIO fileIO = FileIO ?? throw new InvalidOperationException("FileIO cannot be null");
            IBeatmapHasher hasher = new Hasher();
            songLocations.AddRange(config.BeatSaberInstallLocations.Where(l => l.Enabled && l.IsValid()));
            songLocations.AddRange(config.AlternateSongsPaths.Where(l => l.Enabled && l.IsValid()));

            services.AddSingleton(hasher);
            foreach (ISongLocation location in songLocations)
            {

                var songTarget = await CreateTarget(location, fileIO, hasher, LogFactory);
                services.AddSingleton(songTarget);
            }
        }

        private async Task<IBeatmapsTarget> CreateTarget(ISongLocation location, FileIO fileIO, IBeatmapHasher hasher, ILogFactory? logFactory)
        {
            bool overwriteTarget = false;
            bool unzipBeatmaps = true;
            if (location is CustomSongLocation customLocation)
            {
                unzipBeatmaps = customLocation.UnzipBeatmaps;
            }
            HistoryManager? historyManager = null;
            PlaylistManager? playlistManager = null;
            if (!string.IsNullOrEmpty(location.HistoryPath))
            {
                string historyPath = location.FullHistoryPath;
                string historyDirectory = Path.GetDirectoryName(historyPath) ?? string.Empty;
                try
                {
                    Directory.CreateDirectory(historyDirectory);
                    historyManager = new HistoryManager(historyPath, fileIO, LogFactory);
                    historyManager.Initialize();
                }
                catch (Exception ex)
                {
                    Logger?.Info($"Unable to initialize HistoryManager at '{historyPath}': {ex.Message}");
                }
            }
            if (!string.IsNullOrEmpty(location.PlaylistDirectory))
            {
                string playlistDirectory = location.FullPlaylistsPath;
                Directory.CreateDirectory(playlistDirectory);
                playlistManager = new PlaylistManager(playlistDirectory, new LegacyPlaylistHandler(), new BlistPlaylistHandler());
            }
            string songsDirectory = location.FullSongsPath;
            Directory.CreateDirectory(songsDirectory);
            ISongHashCollection? hashCollection = new DirectoryHashCollection(songsDirectory, hasher, logFactory);
            Stopwatch sw = new Stopwatch();
            Logger?.Info($"Hashing beatmaps in '{Paths.GetRelativeDirectory(songsDirectory)}'...");
            sw.Start();
            int hashedCount = await hashCollection.RefreshHashesAsync(false, null, CancellationToken.None).ConfigureAwait(false);
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            Logger?.Info($"Hashed {hashedCount} beatmaps in {Paths.GetRelativeDirectory(songsDirectory)} in {FormatTimeSpan(ts)}.");
            IBeatmapsTarget songTarget = new DirectoryTarget(songsDirectory, overwriteTarget, unzipBeatmaps, 
                fileIO, hashCollection, hasher, historyManager, playlistManager);
            return songTarget;
        }

        public async Task Run(string[] args)
        {
            bool breakEarly = false;
            ArgsParser argsParser = new ArgsParser();
            argsParser.ParseArgs(args);
            Options options = argsParser.Options;

            SetupLogging(options.LogDirectory);

            Logger.Info($"Starting BeatSyncConsole v{VersionString} |{VersionInfo.Description}");

            foreach (string msg in argsParser.ArgErrorMsgs)
            {
                Logger.Error(msg);
            }
            foreach (var errors in argsParser.ArgErrors)
            {
                if (errors.IsVersion())
                    breakEarly = true;
                if (errors.IsHelp())
                    breakEarly = true;
            }
            foreach (string msg in argsParser.ArgDebugMsgs)
            {
                Logger.Debug(msg);
            }
            await CheckVersion().ConfigureAwait(false);
            if (breakEarly)
                return;

            var configManager = new ConfigManager(options.ConfigDirectory);
            bool validConfig = await configManager.InitializeConfigAsync();
            if (!validConfig || configManager.Config == null)
            {
                Logger.Info("BeatSyncConsole cannot run without a valid config, exiting.");
                return;
            }
            ConfigureServices(services, configManager.Config);
            await ConfigureTargetsAsync(services, configManager.Config).ConfigureAwait(false);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
        }


        private async Task CheckVersion()
        {
            try
            {
                VersionChecker versionChecker = new VersionChecker
                {
                    ReleaseFilter = VersionChecker.HasAsset("BeatSyncConsole")
                };
                GithubVersion latest = await versionChecker.GetLatestVersionAsync("Zingabopp", "BeatSync").ConfigureAwait(false);
                if (!latest.IsValid)
                {
                    Logger.Warning($"Unable to get information on the latest version.");
                    return;
                }
                Version? current = Assembly.GetExecutingAssembly().GetName().Version;
                if (current != null)
                {
                    int compare = latest.CompareTo(current);
                    if (compare > 0)
                        Logger.Warning($"There is a new version of BeatSyncConsole available: ({latest}). Download the latest release from '{ReleaseUrl}'.");
                    else if (compare < 0)
                        Logger.Info($"Running a build of BeatSyncConsole from the future! Current released version is {latest}.");
                    else
                        Logger.Info($"Running the latest release of BeatSyncConsole.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error checking for latest version: {ex.Message}.");
                Logger.Debug(ex);
            }

        }


        public static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalMinutes >= 1)
            {
                return string.Format("{0}m {1}s", (int)ts.TotalMinutes, ts.Seconds);
            }
            else
                return string.Format("{0}.{1:D2}s", ts.Seconds, (int)Math.Round(ts.Milliseconds / 10d));
        }
    }
}
