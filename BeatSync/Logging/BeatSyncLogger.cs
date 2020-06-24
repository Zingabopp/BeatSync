using BeatSyncLib.Logging;
using System;
using IPALogger = IPA.Logging.Logger;

namespace BeatSync.Logging
{
    public class BeatSyncLogger : BeatSyncLoggerBase
    {
        protected IPALogger IPALogger;

        public readonly string? SourceName;
        public BeatSyncLogger(IPALogger logger, string? sourceName = null)
        {
            IPALogger = logger;
            SourceName = sourceName;
        }

        public override void Log(string message, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            if (!string.IsNullOrEmpty(SourceName))
                IPALogger.Log(logLevel.ToIPALogLevel(), message);
            else
                IPALogger.Log(logLevel.ToIPALogLevel(), message);
        }

        public override void Log(Exception ex, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            if (!string.IsNullOrEmpty(SourceName))
                IPALogger.Log(logLevel.ToIPALogLevel(), ex);
            else
                IPALogger.Log(logLevel.ToIPALogLevel(), $"[{SourceName}]: {ex}");
        }
    }
}
