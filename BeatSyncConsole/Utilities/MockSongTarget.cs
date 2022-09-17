using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncConsole.Utilities
{
    public class MockSongTarget : BeatmapTarget
    {
        protected static Task<BeatmapState> State_NotWanted = Task.FromResult(BeatmapState.NotWanted);
        public override string TargetName => "MockSongTarget";

        public override Task<BeatmapState> CheckSongExistsAsync(string songHash) => State_NotWanted;

        public override Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
