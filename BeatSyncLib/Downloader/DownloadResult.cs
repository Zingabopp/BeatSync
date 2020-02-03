using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public class DownloadResult
        : IDisposable
    {
        public DownloadResult(DownloadContainer container, DownloadResultStatus status, int httpStatus, string reason = null, Exception exception = null)
        {
            DownloadContainer = container;
            Status = status;
            HttpStatusCode = httpStatus;
            Reason = reason;
            Exception = exception;
        }
        public bool IsDisposed { get; protected set; }
        public DownloadContainer DownloadContainer { get; protected set; }
        public string Reason { get; protected set; }
        public DownloadResultStatus Status { get; protected set; }
        public int HttpStatusCode { get; protected set; }
        public Exception Exception { get; protected set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposedValue)
            {
                if (disposing)
                {
                    DownloadContainer.Dispose();
                    DownloadContainer = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public abstract class DownloadContainer
        : IDisposable
    {
        public abstract bool ResultAvailable { get; }
        public bool IsDisposed { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public long ReceiveData(Stream inputStream) => ReceiveData(inputStream, false);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public abstract long ReceiveData(Stream inputStream, bool disposeInput);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream) => ReceiveDataAsync(inputStream, CancellationToken.None);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream, CancellationToken cancellationToken) => ReceiveDataAsync(inputStream, false, cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public abstract Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, CancellationToken cancellationToken);
        public abstract bool TryGetResultStream(out Stream stream, out Exception exception);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected abstract void Dispose(bool disposing);
    }

    public class DownloadFileContainer : DownloadContainer
    {
        public string FilePath { get; private set; }
        public bool Overwrite { get; private set; }
        public DownloadFileContainer(string filePath, bool overwrite = true)
        {
            FilePath = filePath;
            Overwrite = overwrite;
        }

        public override bool ResultAvailable { get => File.Exists(FilePath); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public override long ReceiveData(Stream inputStream, bool disposeInput)
        {
            try
            {
                FileStream fileStream = null;
                FileMode fileMode = Overwrite ? FileMode.Create : FileMode.CreateNew;

                fileStream = new FileStream(FilePath, fileMode, FileAccess.Write, FileShare.None);
                inputStream.CopyTo(fileStream, 81920);
                return fileStream.Length;
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public async override Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, CancellationToken cancellationToken)
        {
            try
            {
                FileStream fileStream = null;

                FileMode fileMode = Overwrite ? FileMode.Create : FileMode.CreateNew;
                fileStream = new FileStream(FilePath, fileMode, FileAccess.Write, FileShare.None);
                await inputStream.CopyToAsync(fileStream, 81920, cancellationToken).ConfigureAwait(false);
                long fileStreamLength = fileStream.Length;
                fileStream.Close();
                return fileStreamLength;
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

        public override bool TryGetResultStream(out Stream stream, out Exception exception)
        {
            stream = null;
            try
            {
                stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                exception = null;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        ~DownloadFileContainer()
        {
            Dispose(false);
        }
        bool disposed;
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposed)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
                try
                {
                    string file = FilePath;
                    FilePath = null;
                    if (!string.IsNullOrEmpty(file))
                    {
                        File.Delete(file);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                disposed = true;
            }
        }

    }

    public class DownloadMemoryContainer : DownloadContainer
    {
        private byte[] _data;

        public override bool ResultAvailable { get => _data != null; }

        public DownloadMemoryContainer() { }
        public DownloadMemoryContainer(byte[] existingData) => _data = existingData;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public override long ReceiveData(Stream inputStream, bool disposeInput)
        {
            try
            {
                if (inputStream is MemoryStream memoryStream)
                {
                    _data = memoryStream.ToArray();
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    _data = ms.ToArray();
                }
                return _data?.Length ?? 0;
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public async override Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, CancellationToken cancellationToken)
        {
            try
            {
                if (inputStream is MemoryStream ms)
                {
                    _data = ms.ToArray();
                }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
                    _data = memoryStream.ToArray();
                    long streamLength = memoryStream.Length;
                    return streamLength;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

        public override bool TryGetResultStream(out Stream stream, out Exception exception)
        {
            stream = null;
            try
            {
                stream = new MemoryStream(_data);
                exception = null;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        bool disposed;
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposed)
            {
                if (disposing)
                {

                }
                _data = null;
                disposed = true;
            }
        }
    }

    public enum DownloadResultStatus
    {
        Unknown = 0,
        Success = 1,
        NetFailed = 2,
        IOFailed = 3,
        InvalidRequest = 4,
        NetNotFound = 5,
        Canceled = 6
    }
}
