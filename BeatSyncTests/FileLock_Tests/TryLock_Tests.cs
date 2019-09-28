using BeatSync.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncTests.FileLock_Tests
{
    [TestClass]
    public class TryLock_Tests
    {
        private string OutputDir = Path.GetFullPath(@"Output\FileLockTests");

        [TestMethod]
        public void TryLock_NoLock()
        {
            var dir = Path.Combine(OutputDir, "NoLock");
            var fileLock = new FileLock(dir);
            if(File.Exists(fileLock.LockFile))
                File.Delete(fileLock.LockFile);
            Assert.IsFalse(File.Exists(fileLock.LockFile));
            var locked = fileLock.TryLock();
            Assert.IsTrue(locked);
            Assert.IsTrue(File.Exists(fileLock.LockFile));
            fileLock.Dispose();
            Assert.IsFalse(File.Exists(fileLock.LockFile));
        }

        [TestMethod]
        public void TryLock_AlreadyLocked()
        {
            var dir = Path.Combine(OutputDir, "AlreadyLocked");
            var existingLock = new FileLock(dir);
            if (File.Exists(existingLock.LockFile))
                File.Delete(existingLock.LockFile);
            Assert.IsFalse(File.Exists(existingLock.LockFile));
            var locked = existingLock.TryLock();
            Assert.IsTrue(locked);
            Assert.IsTrue(File.Exists(existingLock.LockFile));

            var newLock = new FileLock(dir);
            var didLock = newLock.TryLock();
            Assert.IsFalse(didLock);
        }

        [TestMethod]
        public void TryLock_ExistingFile_NotLocked()
        {
            var dir = Path.Combine(OutputDir, "ExistingFile_NotLocked");
            Directory.CreateDirectory(dir);
            var fileLock = new FileLock(dir);
            File.Create(fileLock.LockFile).Close();
            Assert.IsTrue(File.Exists(fileLock.LockFile));
            var locked = fileLock.TryLock();
            Assert.IsTrue(locked);
            Assert.IsTrue(File.Exists(fileLock.LockFile));
            fileLock.Dispose();
            Assert.IsFalse(File.Exists(fileLock.LockFile));
        }
    }
}
