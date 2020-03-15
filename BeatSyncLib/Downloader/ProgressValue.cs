using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Utilities;
using static BeatSyncLib.Utilities.Util;

namespace BeatSyncLib.Downloader
{
    public struct ProgressValue
    {
        public long? ExpectedMax;
        public long TotalProgress;
        public double? ProgressPercentage => ExpectedMax != null && ExpectedMax != 0 ? TotalProgress / ExpectedMax : null;
        public ProgressValue(long totalProgress, long? expectedMax)
        {
            TotalProgress = totalProgress;
            ExpectedMax = expectedMax;
            _stringVal = null;
        }
        private ByteUnit ByteSize => ByteUnit.Megabyte;

        private string _stringVal;
        private string stringVal
        {
            get
            {
                if (!string.IsNullOrEmpty(_stringVal))
                    return _stringVal;
                if (ExpectedMax == null)
                    _stringVal = $"{ConvertByteValue(TotalProgress, ByteSize).ToString("N2")} MB/?";
                else
                    _stringVal = $"({ProgressPercentage.Value.ToString("P2")}) {ConvertByteValue(TotalProgress, ByteSize).ToString("N2")} MB/{ConvertByteValue(ExpectedMax.Value, ByteSize).ToString("N2")} MB";
                return _stringVal;
            }
        }
        public override string ToString()
        {
            return stringVal;
        }
    }
}
