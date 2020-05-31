using BeatSyncLib;
using BeatSyncLib.Logging;
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
            catch(Exception ex)
            {
                ConsoleColor previousColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error in Logging thread: {ex.Message}");
                Console.WriteLine(ex);
                Console.ForegroundColor = previousColor;
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
                    logger.Write(message.Message, message.LogLevel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to {typeof(Logger)}: {ex.Message}");
                    Console.WriteLine($"Error writing to {typeof(Logger)}: {ex.StackTrace}");
                }
            }
        }

        internal static void Stop()
        {
            cts.Cancel();
        }
    }

    public struct LogMessage
    {
        public string Message;
        public LogLevel LogLevel;
        public bool RequiresInput;
    }
}
