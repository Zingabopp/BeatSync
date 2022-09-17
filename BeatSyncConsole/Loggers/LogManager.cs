using BeatSyncLib;
using SongFeedReaders.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;

namespace BeatSyncConsole.Loggers
{
    public static class LogManager
    {
        private static BlockingCollection<LogMessage> QueuedMessages = new BlockingCollection<LogMessage>();
        private static ILogWriter[] LogWriters = Array.Empty<ILogWriter>();
        static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly Thread logThread;
        public static bool IsAlive { get; private set; }
        private static bool InputAvailable;
        private static string? LastInput;
        public static bool HasWriters => LogWriters.Length > 0;
        static LogManager()
        {
            logThread = new Thread(Run);
            logThread.Name = "Logging";
            logThread.Start();
        }
        public static void QueueMessage(LogMessage message)
        {
            if (!IsAlive)
                throw new InvalidOperationException("LogManager is dead.");
            QueuedMessages.Add(message);
        }
        public static void QueueMessage(string message, LogLevel logLevel)
        {
            if (!IsAlive)
                throw new InvalidOperationException("LogManager is dead.");
            QueuedMessages.Add(new LogMessage() { Message = message, LogLevel = logLevel });
        }
        public static string? GetUserInput(string message)
        {
            QueueMessage(new LogMessage() { Message = message, LogLevel = LogLevel.Info, RequiresInput = true });
            while (!InputAvailable)
                Thread.Sleep(1);
            string? response = LastInput;
            LastInput = null;
            InputAvailable = false;
            return response;
        }

        public static void AddLogWriter(ILogWriter logWriter)
        {
            LogWriters = LogWriters.Append(logWriter).ToArray();
        }

        public static void RemoveWriter(ILogWriter logWriter, string? reason = null, LogLevel logLevel = LogLevel.Info)
        {
            if (LogWriters.Contains(logWriter))
            {
                LogWriters = LogWriters.Where(w => w != logWriter).ToArray();
                if (!string.IsNullOrEmpty(reason))
                {
                    string message = $"Removed {logWriter.GetType().Name}: {reason}";
                    if (IsAlive && HasWriters)
                    {
                        QueueMessage(message, logLevel);
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
        }

        private static void Run()
        {
            IsAlive = true;
            try
            {
                foreach (var message in QueuedMessages.GetConsumingEnumerable(cts.Token))
                {
                    if (message.RequiresInput)
                    {
                        while (InputAvailable)
                            Thread.Sleep(1);
                        Console.WriteLine(message.Message);
                        LastInput = Console.ReadLine();
                        InputAvailable = true;
                    }
                    else
                        WriteToAll(message);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ConsoleWriteError($"Error in Logging thread: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsAlive = false;
            }
        }

        private static void WriteToAll(LogMessage message)
        {
            foreach (var logger in LogWriters)
            {
                try
                {
                    logger.Write(message);
                }
                catch (Exception ex)
                {
                    RemoveWriter(logger, "Caused error.", LogLevel.Error);
                    ConsoleWriteError($"Error writing to {logger?.GetType()}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static void ConsoleWriteError(string message)
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        internal static void Abort()
        {
            if (IsAlive)
                QueuedMessages.TryAdd(new LogMessage() { Message = "Aborting LogManager.", LogLevel = LogLevel.Info });
            IsAlive = false;
            QueuedMessages.CompleteAdding();
            cts.Cancel();
        }

        internal static void Stop()
        {
            if (IsAlive)
                QueuedMessages.TryAdd(new LogMessage() { Message = "Stopping LogManager.", LogLevel = LogLevel.Info });
            IsAlive = false;
            QueuedMessages.CompleteAdding();
        }
        internal static void Wait()
        {
            logThread.Join();
        }
    }
    // 0    5--8 9 11
    // Text Text Text
    public struct LogMessage
    {
        public string Message;
        public LogLevel LogLevel;
        public bool RequiresInput;
        public ColoredSection[]? ColoredSections;
    }

    public struct ColoredSection
    {
        public ColoredSection(int startIndex, int length, ConsoleColor color)
        {
            StartIndex = Math.Max(0, startIndex);
            Length = Math.Max(0, length);
            Color = color;
        }
        public int StartIndex;
        public int Length;
        public ConsoleColor Color;
    }
}
