using BeatSyncLib.Utilities;
using SongFeedReaders.Logging;
using WebUtilities;
using WebUtilities.HttpClientWrapper;

namespace BeatSyncLibTests
{
    public static class TestSetup
    {
        public static ILogFactory LogFactory { get; private set; } = null!;
        public static IWebClient WebClient { get; private set; } = null!;
        public static FileIO FileIO { get; private set; } = null!;
        public static void Initialize()
        {
            if (LogFactory == null)
                LogFactory = new LogFactory((m, s) => new FeedReaderLogger(m), new LoggerSettings()
                {
                    LogLevel = LogLevel.Debug,
                    ModuleName = "BeatSyncLibTests",
                    EnableTimeStamp = true,
                    ShowModule = true
                });
            if (WebClient == null)
                WebClient = new HttpClientWrapper("BeatSyncLibTests/1.0.0");
            FileIO = new FileIO(WebClient, LogFactory);
        }
    }
}
