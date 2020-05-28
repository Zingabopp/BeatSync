using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncPlaylists.Logging;
namespace BeatSyncPlaylistsTests
{
    public class TestPlaylistLogger : BeatSyncPlaylistLogger
    {
        public override void Log(string message, LogLevel logLevel)
        {
            Console.WriteLine($"[{logLevel}] - {message}");
        }

        public override void Log(Exception ex, LogLevel logLevel)
        {
            Console.WriteLine($"[{logLevel}] - {ex}");
        }
    }
}
