using BeatSync;
using BeatSync.Logging;
using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncTests
{
    public static class TestSetup
    {
        public static void Initialize()
        {
            if(Logger.log == null)
                Logger.log = new BeatSyncConsoleLogger();
            LoggingController.DefaultLogger = new BeatSyncFeedReaderLogger(LoggingController.DefaultLogController);
        }
    }
}
