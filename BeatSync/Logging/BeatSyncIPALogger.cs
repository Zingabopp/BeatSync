using System;
using IPALogger = IPA.Logging.Logger;

namespace BeatSync.Logging
{
    public class BeatSyncIPALogger : IBeatSyncLogger
    {
        private IPALogger logSource;
        public LogLevel LoggingLevel { get; set; }

        public BeatSyncIPALogger(IPALogger logger, LogLevel logLevel = LogLevel.Debug)
        {
            logSource = logger;
            LoggingLevel = logLevel;
        }

        public void Debug(string message)
        {
            logSource.Debug(message);
        }

        public void Debug(Exception ex)
        {
            logSource.Debug(ex);
        }

        public void Info(string message)
        {
            logSource.Info(message);
        }

        public void Info(Exception ex)
        {
            logSource.Info(ex);
        }

        public void Warn(string message)
        {
            logSource.Warn(message);
        }

        public void Warn(Exception ex)
        {
            logSource.Warn(ex);
        }

        public void Critical(string message)
        {
            logSource.Critical(message);
        }

        public void Critical(Exception ex)
        {
            logSource.Critical(ex);
        }

        public void Error(string message)
        {
            logSource.Error(message);
        }

        public void Error(Exception ex)
        {
            logSource.Error(ex);
        }
    }
}
