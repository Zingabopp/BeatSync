using BeatSyncLib.Logging;
using System;

namespace BeatSyncConsole.Loggers
{
    public class BeatSyncLibLogger : BeatSyncLoggerBase
    {
        public override void Log(string message, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            Console.WriteLine($"[BeatSyncLib]: {message}");
        }

        public override void Log(Exception ex, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            Console.WriteLine($"[BeatSyncLib]: {ex}");
        }
    }
}
