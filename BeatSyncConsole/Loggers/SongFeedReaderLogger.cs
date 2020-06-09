using SongFeedReaders.Logging;
using System;

namespace BeatSyncConsole.Loggers
{
    public class SongFeedReaderLogger : FeedReaderLoggerBase
    {
        public readonly string SourceName;
        public SongFeedReaderLogger(string sourceName)
        {
            SourceName = sourceName;
        }
        public override void Log(string message, LogLevel logLevel, string file, string member, int line)
        {
            if (LogLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {message}", ConvertLogLevel(logLevel));
        }

        public override void Log(string message, Exception ex, LogLevel logLevel, string file, string member, int line)
        {
            if (LogLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {ex}", ConvertLogLevel(logLevel));
        }

        public static BeatSyncLib.Logging.LogLevel ConvertLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => BeatSyncLib.Logging.LogLevel.Debug,
                LogLevel.Debug => BeatSyncLib.Logging.LogLevel.Debug,
                LogLevel.Info => BeatSyncLib.Logging.LogLevel.Info,
                LogLevel.Warning => BeatSyncLib.Logging.LogLevel.Warn,
                LogLevel.Error => BeatSyncLib.Logging.LogLevel.Error,
                LogLevel.Exception => BeatSyncLib.Logging.LogLevel.Error,
                LogLevel.Disabled => BeatSyncLib.Logging.LogLevel.Disabled,
                _ => BeatSyncLib.Logging.LogLevel.Debug,
            };
        }
    }
}
