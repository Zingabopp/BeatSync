using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public static class Logger
    {
        public static readonly BeatSyncLogger log = new BeatSyncLogger("BeatSyncConsole");
    }
}
