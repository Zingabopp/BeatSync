using SongFeedReaders.Logging;
using System;
using System.Runtime.CompilerServices;

namespace BeatSync.Logging
{
    public class BeatSyncFeedReaderLogger : FeedReaderLoggerBase
    {
        private BeatSyncFeedReaderLogger()
        {
            LoggerName = "BeatSync";
        }

        public BeatSyncFeedReaderLogger(LoggingController controller)
            : this()
        {
            LogController = controller;
        }
        public override void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Debug)
                return;
            Logger.log?.Debug(message);
        }

        public override void Error(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Error)
                return;
            Logger.log?.Error(message);
        }

        public override void Exception(string message, Exception e, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Exception)
                return;
            Logger.log?.Error($"{message}: {e.Message}");
            Logger.log?.Debug(e);
        }

        public override void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Info)
                return;
            Logger.log?.Info(message);
        }

        public override void Trace(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Trace)
                return;
            Logger.log?.Debug(message);
        }

        public override void Warning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > SongFeedReaders.Logging.LogLevel.Warning)
                return;
            Logger.log?.Warn(message);
        }
    }
}
