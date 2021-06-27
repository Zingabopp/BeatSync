using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public static class Logger
    {
        internal const string _timePattern = "yyyy-MM-dd HH:mm:ss";
        internal static bool _useUtcTime = false;
        internal static DateTime GetTime()
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
        internal static string GetCurrentTime() => GetTime().ToString(_timePattern);
        public static readonly BeatSyncLogger log = new BeatSyncLogger("BeatSyncConsole");
    }
}
