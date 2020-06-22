using BeatSyncLib.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BeatSyncConsole.Loggers
{
    public class BeatSyncLogger : BeatSyncLoggerBase
    {
        public readonly string SourceName;
        public BeatSyncLogger(string sourceName)
        {
            SourceName = sourceName;
        }
        public void Log(string message, LogLevel logLevel, IEnumerable<ColoredSection> coloredSections)
        {
            if (LoggingLevel > logLevel)
                return;
            string prefix = $"[{SourceName} - {logLevel}]: ";
            ColoredSection[]? sections = coloredSections?.ToArray();
            if(sections != null && sections.Length > 0)
            {
                int prefixLength = prefix.Length;
                for(int i = 0; i < sections.Length; i++)
                {
                    sections[i].StartIndex += prefixLength;
                }
            }
            LogMessage logMessage = new LogMessage()
            {
                Message = $"{prefix}{message}",
                LogLevel = logLevel,
                ColoredSections = sections
            };
            LogManager.QueueMessage(logMessage);
        }

        public override void Log(string message, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {message}", logLevel);
        }

        public override void Log(Exception ex, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {ex}", logLevel);
        }
    }
}
