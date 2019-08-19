using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Logging
{
    public interface IBeatSyncLogger
    {
        LogLevel LoggingLevel { get; set; }

        void Debug(string message);
        void Debug(Exception ex);
        void Info(string message);
        void Info(Exception ex);
        void Warn(string message);
        void Warn(Exception ex);
        void Error(string message);
        void Error(Exception ex);
        void Critical(string message);
        void Critical(Exception ex);
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Critical = 3,
        Error = 4,
        Disabled = 5
    }
}
