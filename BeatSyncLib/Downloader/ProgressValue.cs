using static BeatSyncLib.Utilities.Util;

namespace BeatSyncLib.Downloader
{
    public struct ProgressValue
    {
        public readonly long? ExpectedMax;
        public readonly long TotalProgress;
        public double? ProgressPercentage => ExpectedMax != null && ExpectedMax != 0
                                                ? (double)TotalProgress / ExpectedMax
                                                : null;
        public ProgressValue(long totalProgress, long? expectedMax)
        {
            TotalProgress = totalProgress;
            ExpectedMax = expectedMax;
            _stringVal = null;
        }
        private ByteUnit ByteSize => ByteUnit.Megabyte;

        private string? _stringVal;
        private string stringVal
        {
            get
            {
                if (_stringVal != null && _stringVal.Length > 0)
                    return _stringVal;
                if (ExpectedMax == null || ProgressPercentage == null)
                    _stringVal = $"{ConvertByteValue(TotalProgress, ByteSize).ToString("N2")} MB/?";
                else
                {
                    double value = ProgressPercentage.Value;
                    _stringVal = $"({value.ToString("P2")}) {ConvertByteValue(TotalProgress, ByteSize).ToString("N2")} MB/{ConvertByteValue(ExpectedMax.Value, ByteSize).ToString("N2")} MB";
                }
                return _stringVal;
            }
        }
        public override string ToString()
        {
            return stringVal;
        }
    }
}
