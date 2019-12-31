using System;
using System.IO;
namespace BeatSyncLib.Utilities
{
    /// <summary>
    /// Used to indicate BeatSync is running in a directory (Synchronize access with SyncSaberConsole sometime in the future).
    /// </summary>
    public class FileLock : IDisposable
    {
        private const string DefaultName = "_beatSync.lck";
        private string FileName = DefaultName;
        private string DirectoryPath;
        private FileStream FileHandle;
        public string LockFile
        {
            get { return Path.Combine(DirectoryPath, FileName); }
        }


        public FileLock(string directoryPath)
        {
            DirectoryPath = Path.GetFullPath(directoryPath);
        }

        public bool TryLock()
        {
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
                return false;
            }
        }

        public void Unlock()
        {
            if(FileHandle != null)
            {
                FileHandle.Close();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    FileHandle.Close();
                    try
                    {
                        File.Delete(LockFile);
                    }catch(Exception ex)
                    {
                        Logger.log?.Debug($"Unable to delete lock file {LockFile}: {ex.Message}");
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
