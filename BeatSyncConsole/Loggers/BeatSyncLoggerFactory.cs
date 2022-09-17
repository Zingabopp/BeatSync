using SongFeedReaders.Logging;
using System;

namespace BeatSyncConsole.Loggers
{
    public class BeatSyncLoggerFactory : ILogFactory
    {
        public BeatSyncLoggerSettings LoggerSettings { get; private set; }
        public BeatSyncLoggerFactory(BeatSyncLoggerSettings loggerSettings)
        {
            LoggerSettings = loggerSettings ?? throw new ArgumentNullException(nameof(loggerSettings));
        }
        public ILogger GetLogger(string? moduleName = null)
        {
            BeatSyncLoggerSettings settings = new BeatSyncLoggerSettings(LoggerSettings)
            {
                ModuleName = moduleName
            };
            BeatSyncLogger logger = new BeatSyncLogger(settings);

            return logger;
        }
    }
}
