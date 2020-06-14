using SongFeedReaders.Logging;
using System;
using System.Runtime.CompilerServices;

namespace BeatSync.Logging
{
    public class BeatSyncFeedReaderLogger : FeedReaderLoggerBase
    {
#if DEBUG
        private const string MessagePrefix = "-SongFeedReaders-: ";
#else
        private const string MessagePrefix = "";
#endif
        private BeatSyncFeedReaderLogger()
        {
            LoggerName = "BeatSync";
        }

        public BeatSyncFeedReaderLogger(LoggingController controller)
            : this()
        {
            LogController = controller;
        }

        public override void Log(string message, SongFeedReaders.Logging.LogLevel logLevel, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Error)
                return;
            Logger.log?.Error(MessagePrefix + message);
        }

        public override void Log(string message, Exception e, SongFeedReaders.Logging.LogLevel logLevel, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (e == null)
                return;
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Exception)
                return;
            if (!string.IsNullOrEmpty(message))
                Logger.log?.Error($"{MessagePrefix + message}: {e.Message}");
            else
                Logger.log?.Error(e.Message);
            Logger.log?.Debug(e);
        }
    }
}
