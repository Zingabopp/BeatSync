using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncPlaylistsTests
{
    public static class TestSetup
    {
        private static object _setupLock = new object();
        private static bool setupComplete = false;
        public static void Setup()
        {
            lock (_setupLock)
            {
                if (setupComplete)
                    return;
                BeatSyncPlaylists.Logging.Logger.log = new TestPlaylistLogger();
                setupComplete = true;
            }
        }
    }
}
