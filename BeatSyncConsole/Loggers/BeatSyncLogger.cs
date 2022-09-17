using SongFeedReaders.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BeatSyncConsole.Loggers
{
    public sealed class BeatSyncLogger : ILogger
    {
        private const string _timePattern = "yyyy-MM-dd HH:mm:ss";
        private static bool _useUtcTime = false;
        private static DateTime GetTime()
        {
            if (_useUtcTime)
                return DateTime.UtcNow;
            try
            {
                return DateTime.Now;
            }
            catch
            {
                _useUtcTime = true;
                return DateTime.UtcNow;
            }
        }
        private static string GetCurrentTime() => GetTime().ToString(_timePattern);

        public ILoggerSettings Settings { get; set; }
        public BeatSyncLogger(ILoggerSettings settings)
        {
            Settings = settings;
        }
        public void Log(string message, LogLevel logLevel, IEnumerable<ColoredSection>? coloredSections, [CallerFilePath] string? file = null,
            [CallerMemberName] string? member = null, [CallerLineNumber] int line = 0)
        {
            if (Settings.LogLevel > logLevel)
                return;
            string logLevelStr = logLevel.ToString();
            string moduleSection = Settings.ShowModule && !string.IsNullOrWhiteSpace(Settings.ModuleName) ? $" | {Settings.ModuleName}" : string.Empty;
            string prefix = $"[{logLevelStr} @ {GetCurrentTime()}{moduleSection}]: ";
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

        public void Log(string message, LogLevel logLevel, [CallerFilePath] string? file = null,
            [CallerMemberName] string? member = null, [CallerLineNumber] int line = 0)
            => Log(message, logLevel, null, file, member, line);

        public void Log(Exception ex, LogLevel logLevel, [CallerFilePath] string? file = null,
            [CallerMemberName] string? member = null, [CallerLineNumber] int line = 0) 
            => Log(ex.ToString(), logLevel, null, file, member, line);
    }

    public static class BeatSyncLoggerExtensions
    {
        public static void Log(this ILogger logger, string message, LogLevel logLevel, IEnumerable<ColoredSection>? coloredSections)
        {
            if(logger is BeatSyncLogger bsl)
            {
                bsl.Log(message, logLevel, coloredSections);
            }
            else
                logger.Log(message, logLevel);
        }

    }

}
