using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public void Write(LogMessage logMessage)
        {
            if (LogLevel > logMessage.LogLevel)
                return;
            ConsoleColor previousColor = Console.ForegroundColor;

            if (ColoredConsoleText)
            {
                ConsoleColor mainColor = LogLevelColor(logMessage.LogLevel, previousColor);
                
                ColoredSection[]? originalSections = logMessage.ColoredSections?
                    .OrderBy(c => c.StartIndex)
                    .Where(c => c.StartIndex < logMessage.Message.Length)
                    .ToArray();

                if(originalSections != null && originalSections.Length > 0)
                {
                    string message = logMessage.Message;
                    int startIndex = 0;
                    int lastIndex = 0;
                    foreach (var section in originalSections)
                    {
                        if(startIndex < section.StartIndex)
                        {
                            Console.ForegroundColor = mainColor;
                            lastIndex = section.StartIndex;
                            Console.Write(message[startIndex..lastIndex]);
                            startIndex = lastIndex;
                        }
                        lastIndex = Math.Min(message.Length, section.StartIndex + section.Length);
                        Console.ForegroundColor = section.Color;
                        Console.Write(message[startIndex..lastIndex]);
                        startIndex = lastIndex;
                    }
                    if(startIndex < message.Length)
                    {
                        Console.ForegroundColor = mainColor;
                        Console.Write(message[startIndex..]);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = mainColor;
                    Console.WriteLine(logMessage.Message);
                }


                Console.ForegroundColor = previousColor;
            }
            else
            {
                Console.WriteLine(logMessage.Message);
            }
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
