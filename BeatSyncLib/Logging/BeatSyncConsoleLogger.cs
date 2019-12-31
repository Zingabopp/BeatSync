using System;

namespace BeatSyncLib.Logging
{
    public class BeatSyncConsoleLogger : IBeatSyncLogger
    {
        public LogLevel LoggingLevel { get; set; }

        public void Debug(string message)
        {
            if (LoggingLevel > LogLevel.Debug)
                return;
            Console.WriteLine(message);
        }

        public void Debug(Exception ex)
        {
            if (LoggingLevel > LogLevel.Debug)
                return;
            Console.WriteLine(ex);
        }

        public  void Info(string message)
        {
            if (LoggingLevel > LogLevel.Info)
                return;
            Console.WriteLine(message);
        }

        public void Info(Exception ex)
        {
            if (LoggingLevel > LogLevel.Info)
                return;
            Console.WriteLine(ex);
        }

        public void Warn(string message)
        {
            if (LoggingLevel > LogLevel.Warn)
                return;
            Console.WriteLine(message);
        }

        public void Warn(Exception ex)
        {
            if (LoggingLevel > LogLevel.Warn)
                return;
            Console.WriteLine(ex);
        }

        public void Critical(string message)
        {
            if (LoggingLevel > LogLevel.Critical)
                return;
            Console.WriteLine(message);
        }

        public void Critical(Exception ex)
        {
            if (LoggingLevel > LogLevel.Critical)
                return;
            Console.WriteLine(ex);
        }

        public void Error(string message)
        {
            if (LoggingLevel > LogLevel.Error)
                return;
            Console.WriteLine(message);
        }

        public void Error(Exception ex)
        {
            if (LoggingLevel > LogLevel.Error)
                return;
            Console.WriteLine(ex);
        }
    }
}
