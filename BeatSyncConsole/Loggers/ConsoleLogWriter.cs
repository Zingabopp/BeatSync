using BeatSyncLib.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public class ConsoleLogWriter : ILogWriter
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        bool ColoredConsoleText = true;
        public void Write(string message, LogLevel logLevel)
        {
            if (LogLevel > logLevel)
                return;
            var previousColor = Console.ForegroundColor;
            if (ColoredConsoleText)
            {
                Console.ForegroundColor = LogLevelColor(logLevel, previousColor);
            }
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        private static ConsoleColor LogLevelColor(LogLevel logLevel, ConsoleColor previous)
        {
            return logLevel switch
            {
                LogLevel.Debug => ConsoleColor.Green,
                LogLevel.Info => previous,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Critical => ConsoleColor.Magenta,
                LogLevel.Error => ConsoleColor.Red,
                _ => previous
            };
        }
    }
}
