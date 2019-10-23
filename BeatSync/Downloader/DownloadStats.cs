using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Downloader
{
    public class DownloadStats
    {
        private object _totalLock = new object();
        private object _finishedLock = new object();
        private object _erroredLock = new object();
        private int _totalDownloads;
        private int _finishedDownloads;
        private int _erroredDownloads;
        public int TotalDownloads
        {
            get
            {
                lock (_totalLock)
                    return _totalDownloads;
            }
            private set
            {
                lock (_totalLock)
                    _totalDownloads = value;
            }
        }
        public int FinishedDownloads
        {
            get
            {
                lock (_finishedLock)
                    return _finishedDownloads;
            }
            private set
            {
                lock (_finishedLock)
                    _finishedDownloads = value;
            }
        }
        public int ErroredDownloads
        {
            get
            {
                lock (_erroredLock)
                    return _erroredDownloads;
            }
            private set
            {
                lock (_erroredLock)
                    _erroredDownloads = value;
            }
        }

        public void SetTotalDownloads(int num)
        {
            TotalDownloads = num;
        }
        public void IncrementTotalDownloads()
        {
            TotalDownloads++;
        }
        public void IncrementFinishedDownloads()
        {
            FinishedDownloads++;
        }
        public void IncrementErroredDownloads()
        {
            ErroredDownloads++;
        }
    }
}
