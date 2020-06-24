using SongFeedReaders.Logging;
using System;
using System.Runtime.CompilerServices;

namespace BeatSync.Logging
{
    public class BeatSyncFeedReaderLogger : FeedReaderLoggerBase
    {
#if DEBUG
        private const string MessagePrefix = "[SongFeedReaders]: ";
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
            if (LogLevel > logLevel)
                return;
            Plugin.log?.Log(logLevel.ToIPALogLevel(), MessagePrefix + message);
        }

        public override void Log(string message, Exception e, SongFeedReaders.Logging.LogLevel logLevel, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (e == null)
                return;
            if (LogLevel > logLevel)
                return;
            if (!string.IsNullOrEmpty(message))
                Plugin.log?.Log(logLevel.ToIPALogLevel(), $"{MessagePrefix + message}: {e.Message}");
            else
                Plugin.log?.Log(logLevel.ToIPALogLevel(), e.Message);
            Plugin.log?.Debug(e);
        }
    }
}
