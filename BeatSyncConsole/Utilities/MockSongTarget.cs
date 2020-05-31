using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncConsole.Utilities
{
    public class MockSongTarget : SongTarget
    {
        protected static Task<SongState> State_NotWanted = Task.FromResult(SongState.NotWanted);
        public override string TargetName => "MockSongTarget";

        public override Task<SongState> CheckSongExistsAsync(string songHash) => State_NotWanted;

        public override Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
