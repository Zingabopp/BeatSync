using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Utilities
{
    internal class Logger
    {
        /// <summary>
        /// Static logger. TODO: Get rid of this sometime.
        /// </summary>
        public static readonly ILogger log = new FeedReaderLogger();
    }
}
