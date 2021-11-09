using SongFeedReaders.Logging;
using System;
using System.IO;
namespace BeatSyncLib.Utilities
{
    /// <summary>
    /// Used to indicate BeatSync is running in a directory (Synchronize access with BeatSyncConsole sometime in the future).
    /// </summary>
    public sealed class FileLock : IDisposable
    {
        public const string LockFileName = "_beatSync.lck";
        private readonly string DirectoryPath;
        private FileStream? FileHandle;
        private ILogger? Logger;

        public string LockFile
        {
            get { return Path.Combine(DirectoryPath, LockFileName); }
        }

        public bool IsLocked => FileHandle != null;


        public FileLock(string directoryPath, ILogger? logger = null)
        {
            DirectoryPath = Path.GetFullPath(directoryPath);
            Logger = logger;
        }

        public bool TryLock()
        {
            if(FileHandle != null)
            {
                return true;
            }
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                if (File.Exists(LockFile))
                    File.Delete(LockFile);
                FileHandle = File.Open(LockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (Exception)
            {
                FileHandle = null;
                return false;
            }
        }

        public void Unlock()
        {
            if (FileHandle != null)
            {
                FileHandle.Dispose();
                FileHandle = null;
                try
                {
                    File.Delete(LockFile);
                }
                catch (Exception ex)
                {
                    Logger?.Debug($"Unable to delete lock file {LockFile}: {ex.Message}");
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    FileHandle?.Dispose();
                    FileHandle = null;
                    try
                    {
                        File.Delete(LockFile);
                    }
                    catch(Exception ex)
                    {
                        Logger?.Debug($"Unable to delete lock file {LockFile}: {ex.Message}");
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileLock()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
