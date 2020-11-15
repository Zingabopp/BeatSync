using BeatSyncLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using static BeatSyncLib.Utilities.Util;
namespace BeatSyncLibTests.Utilities
{
    [TestClass]
    public class Util_Tests
    {

        [TestMethod]
        public void GetSongDirectoryName_WithKey()
        {
            string key = "abcd";
            string songName = "TestName";
            string levelAuthorName = "TestAuthor";
            string expected = $"{key} ({songName} - {levelAuthorName})";
            string actual = GetSongDirectoryName(key, songName, levelAuthorName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetSongDirectoryName_InvalidCharacter()
        {
            string key = "abcd";
            string songName = "Test|Name";
            string filteredSongName = "TestName";
            string levelAuthorName = "TestAuthor";
            string expected = $"{key} ({filteredSongName} - {levelAuthorName})";
            string actual = GetSongDirectoryName(key, songName, levelAuthorName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetValidPath_TrimmedWithTrailingSpace()
        {
            string originalPath = @"K:\Oculus\Software\hyperbolic - magnetism - beat - saber\Beat Saber_Data\CustomLevels\4B48(Camellia(Feat.Nanahira) - Can I Friend You On Bassbook L- . - .- . - . - .- ";
            int longestFileName = @"Camellia (Feat. Nanahira) - Can I Friend You On Bassbook Lol [Bassline Yatteru LOL].egg".Length;

            string validPath = FileIO.GetValidPath(originalPath, longestFileName);
            Assert.IsFalse(FileIO.InvalidTrailingPathChars.Any(c => validPath.Last() == c));
        }

        [TestMethod]
        public void GetSongDirectoryName_NoKey()
        {
            string key = null;
            string songName = "TestName";
            string levelAuthorName = "TestAuthor";
            string expected = $"{songName} - {levelAuthorName}";
            string actual = GetSongDirectoryName(key, songName, levelAuthorName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetSongDirectoryName_NoAuthor()
        {
            string key = null;
            string songName = "TestName";
            string levelAuthorName = null;
            string expected = $"{songName}";
            string actual = GetSongDirectoryName(key, songName, levelAuthorName);
            Assert.AreEqual(expected, actual);
        }

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
        public void ConvertByteValue_MegaBytesToMega()
        {
            double expectedValue = 3.54;
            double startingVal = expectedValue;
            double actualValue = ConvertByteValue(startingVal, ByteUnit.Megabyte, 2, ByteUnit.Megabyte);
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
