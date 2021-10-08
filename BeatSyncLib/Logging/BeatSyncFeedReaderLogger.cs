using SongFeedReaders.Logging;
using System;
using System.Runtime.CompilerServices;

namespace BeatSyncLib.Logging
{
    public class BeatSyncFeedReaderLogger : ILogger
    {
#if DEBUG
        private const string MessagePrefix = "-SongFeedReaders-: ";
#else
        private const string MessagePrefix = "";
#endif
        SongFeedReaders.Logging.LogLevel LogLevel { get; set; }
        private BeatSyncFeedReaderLogger()
        {

        }

        public void Log(string message, SongFeedReaders.Logging.LogLevel logLevel,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (LogLevel > logLevel)
                return;
            Logger.log?.Log(message, ConvertLogLevel(logLevel));
        }

        public void Log(Exception e, SongFeedReaders.Logging.LogLevel logLevel
            , [CallerFilePath] string file = "", 
            [CallerMemberName] string member = "", 
            [CallerLineNumber] int line = 0)
        {
            if (LogLevel > logLevel)
                return;
            Logger.log?.Log(e, ConvertLogLevel(logLevel));
        }

        public BeatSyncLib.Logging.LogLevel ConvertLogLevel(SongFeedReaders.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                SongFeedReaders.Logging.LogLevel.Trace => BeatSyncLib.Logging.LogLevel.Debug,
                SongFeedReaders.Logging.LogLevel.Debug => BeatSyncLib.Logging.LogLevel.Debug,
                SongFeedReaders.Logging.LogLevel.Info => BeatSyncLib.Logging.LogLevel.Info,
                SongFeedReaders.Logging.LogLevel.Warning => BeatSyncLib.Logging.LogLevel.Warn,
                SongFeedReaders.Logging.LogLevel.Error => BeatSyncLib.Logging.LogLevel.Error,
                SongFeedReaders.Logging.LogLevel.Exception => BeatSyncLib.Logging.LogLevel.Error,
                SongFeedReaders.Logging.LogLevel.Disabled => BeatSyncLib.Logging.LogLevel.Disabled,
                _ => BeatSyncLib.Logging.LogLevel.Debug,
            };
        }
    }
}
