using BeatSyncLib.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public class FileLogWriter : ILogWriter
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        private readonly StreamWriter LogFile;
        public FileLogWriter(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            LogFile = new StreamWriter(filePath, false);
        }

        public void Write(string message, LogLevel logLevel)
        {
            if (LogLevel > logLevel)
                return;
            LogFile.Write(message + "\n");
            LogFile.Flush();
        }
    }
}
