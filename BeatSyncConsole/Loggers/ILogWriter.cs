using BeatSyncLib.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public interface ILogWriter
    {
        LogLevel LogLevel { get; set; }
        void Write(string message, LogLevel logLevel);
    }
}
