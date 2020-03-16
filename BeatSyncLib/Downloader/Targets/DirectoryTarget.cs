using SongFeedReaders.Data;
using BeatSyncLib.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BeatSyncLib.Hashing;
using System.Threading;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTarget : ISongTarget
    {
        public string TargetName => nameof(DirectoryTarget);

        public bool TransferComplete { get; private set; }
        public string SongHash { get; private set; }
        public string ParentDirectory { get; private set; }
        public string DirectoryName { get; private set; }
        public bool OverwriteTarget { get; private set; }

        public TargetResult TargetResult { get; private set; }

        protected DirectoryTarget(string parentDirectory, string songHash, bool overwriteTarget)
        {
            ParentDirectory = Path.GetFullPath(parentDirectory);
            SongHash = songHash;
            OverwriteTarget = overwriteTarget;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDirectory"></param>
        /// <param name="directoryName"></param>
        /// <param name="songHash"></param>
        /// <param name="overwriteTarget"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DirectoryTarget(string parentDirectory, string directoryName, string songHash, bool overwriteTarget = false)
            : this(parentDirectory, songHash, overwriteTarget)
        {
            if (string.IsNullOrEmpty(parentDirectory))
                throw new ArgumentNullException(nameof(parentDirectory), $"{nameof(parentDirectory)} cannot be null when creating a {nameof(DirectoryTarget)}.");
            if (string.IsNullOrEmpty(directoryName))
                throw new ArgumentNullException(nameof(directoryName), $"{nameof(directoryName)} cannot be null when creating a {nameof(DirectoryTarget)}.");
            DirectoryName = directoryName;
        }

        public DirectoryTarget(string parentDirectory, ISong song, bool overwriteTarget = false)
            : this(parentDirectory, song?.Hash, overwriteTarget)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), $"{nameof(song)} cannot be null when creating a {nameof(DirectoryTarget)}.");
            DirectoryName = Util.GetSongDirectoryName(song.Key, song.Name, song.LevelAuthorName);
        }

        public DirectoryTarget(string parentDirectory, string songHash, string songName, string mapperName, string songKey = null, bool overwriteTarget = false)
            : this(parentDirectory, songHash, overwriteTarget)
        {
            DirectoryName = Util.GetSongDirectoryName(songKey, songName, mapperName);
        }

        public async Task<TargetResult> TransferAsync(Stream sourceStream, CancellationToken cancellationToken)
        {
            string directoryPath = null;
            ZipExtractResult zipResult = null;
            try
            {
                directoryPath = Path.Combine(ParentDirectory, DirectoryName);
                if (!Directory.Exists(ParentDirectory))
                    throw new SongTargetTransferException($"Parent directory doesn't exist: '{ParentDirectory}'");
                zipResult = await Task.Run(() => FileIO.ExtractZip(sourceStream, directoryPath, OverwriteTarget)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(SongHash))
                {
                    string hashAfterDownload = (await SongHasher.GetSongHashDataAsync(zipResult.OutputDirectory).ConfigureAwait(false)).songHash;
                    if (hashAfterDownload != SongHash)
                        throw new SongTargetTransferException($"Extracted song hash doesn't match expected hash: {SongHash} != {hashAfterDownload}");
                }
                TargetResult = new DirectoryTargetResult(TargetName, zipResult.ResultStatus == ZipExtractResultStatus.Success, zipResult, zipResult.Exception);
                return TargetResult;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                TargetResult = new DirectoryTargetResult(TargetName, false, zipResult, ex);
                return TargetResult;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                TransferComplete = true;
            }
        }

        public Task<TargetResult> TransferAsync(Stream sourceStream) => TransferAsync(sourceStream, CancellationToken.None);
    }

    public class DirectoryTargetResult : TargetResult
    {
        public ZipExtractResult ZipExtractResult { get; private set; }
        public DirectoryTargetResult(string targetName, bool success, ZipExtractResult zipExtractResult, Exception exception)
            : base(targetName, success, exception)
        {
            ZipExtractResult = zipExtractResult;
        }
    }
}
