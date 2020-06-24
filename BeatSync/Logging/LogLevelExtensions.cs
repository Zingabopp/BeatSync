using BeatSyncLib.Logging;
using IPALogger = IPA.Logging.Logger;

namespace BeatSync.Logging
{
    public static class LogLevelExtensions
    {
        public static IPALogger.Level ToIPALogLevel(this LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Debug => IPALogger.Level.Debug,
                LogLevel.Info => IPALogger.Level.Info,
                LogLevel.Warn => IPALogger.Level.Warning,
                LogLevel.Critical => IPALogger.Level.Critical,
                LogLevel.Error => IPALogger.Level.Error,
                LogLevel.Disabled => IPALogger.Level.None,
                _ => IPALogger.Level.None
            };
        }
        public static IPALogger.Level ToIPALogLevel(this SongFeedReaders.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                SongFeedReaders.Logging.LogLevel.Debug => IPALogger.Level.Debug,
                SongFeedReaders.Logging.LogLevel.Info => IPALogger.Level.Info,
                SongFeedReaders.Logging.LogLevel.Warning => IPALogger.Level.Warning,
                SongFeedReaders.Logging.LogLevel.Error => IPALogger.Level.Error,
                SongFeedReaders.Logging.LogLevel.Disabled => IPALogger.Level.None,
                _ => IPALogger.Level.None
            };
        }
    }
}
