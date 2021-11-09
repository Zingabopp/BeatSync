using BeatSyncLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncLibTests.Utilities
{
    [TestClass]
    public class FileLock_Tests
    {
        private readonly string OutputPath = Path.Combine("Output", "FileLock");

        [TestMethod]
        public void NoExistingLock()
        {
            string path = Path.Combine(OutputPath, "NoExistingLock");
            bool expectedResult = true;
            FileLock fileLock = new FileLock(path);
            bool result = fileLock.TryLock();

            Assert.AreEqual(expectedResult, result);
            Assert.IsTrue(fileLock.IsLocked);
            Assert.IsTrue(File.Exists(Path.Combine(path, FileLock.LockFileName)));

            fileLock.Unlock();
            Assert.IsFalse(fileLock.IsLocked);
            Assert.IsFalse(File.Exists(Path.Combine(path, FileLock.LockFileName)));
        }

        [TestMethod]
        public void LeftoverLockFile()
        {
            string path = Path.Combine(OutputPath, "LeftoverLockFile");
            string lockFilePath = Path.Combine(path, FileLock.LockFileName);
            Directory.CreateDirectory(path);
            File.Create(lockFilePath).Dispose();
            Assert.IsTrue(File.Exists(lockFilePath));
            bool expectedResult = true;
            FileLock fileLock = new FileLock(path);
            bool result = fileLock.TryLock();

            Assert.AreEqual(expectedResult, result);
            Assert.IsTrue(fileLock.IsLocked);
            Assert.IsTrue(File.Exists(lockFilePath));

            fileLock.Unlock();
            Assert.IsFalse(fileLock.IsLocked);
            Assert.IsFalse(File.Exists(lockFilePath));
        }

        [TestMethod]
        public void ExistingLock()
        {
            string path = Path.Combine(OutputPath, "ExistingLock");
            string lockFilePath = Path.Combine(path, FileLock.LockFileName);
            bool expectedResult = true;
            bool expectedResult2 = false;
            FileLock fileLock = new FileLock(path);
            FileLock fileLock2 = new FileLock(path);
            bool result = fileLock.TryLock();

            Assert.AreEqual(expectedResult, result);
            Assert.IsTrue(fileLock.IsLocked);
            Assert.IsTrue(File.Exists(lockFilePath));

            bool result2 = fileLock2.TryLock();
            Assert.AreEqual(expectedResult2, result2);
            Assert.IsFalse(fileLock2.IsLocked);

            fileLock.Unlock();
            Assert.IsFalse(fileLock.IsLocked);
            Assert.IsFalse(File.Exists(lockFilePath));
        }
    }
}
