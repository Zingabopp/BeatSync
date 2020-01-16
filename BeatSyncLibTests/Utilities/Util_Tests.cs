using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static BeatSyncLib.Utilities.Util;
namespace BeatSyncLibTests.Utilities
{
    [TestClass]
    public class Util_Tests
    {
        [TestMethod]
        public void ConvertByteValue_BytesToMega()
        {
            double expectedValue = 3.54;
            long startingVal = (long)(expectedValue * 1024 * 1024);
            double actualValue = ConvertByteValue(startingVal, ByteUnit.Megabyte);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ConvertByteValue_KiloBytesToMega()
        {
            double expectedValue = 3.54;
            long startingVal = (long)(expectedValue * 1024);
            double actualValue = ConvertByteValue(startingVal, ByteUnit.Megabyte, 2, ByteUnit.Kilobyte);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void ConvertByteValue_ZeroToMega()
        {
            double expectedValue = 0;
            long startingVal = 0;
            double actualValue = ConvertByteValue(startingVal, ByteUnit.Megabyte, 2, ByteUnit.Kilobyte);
            Assert.AreEqual(expectedValue, actualValue);

        }
    }
}
