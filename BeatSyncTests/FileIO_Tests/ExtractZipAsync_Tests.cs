using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using BeatSync.Utilities;
using BeatSync;
using BeatSync.Logging;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;

namespace BeatSyncTests.FileIO_Tests
{
    [TestClass]
    public class ExtractZipAsync_Tests
    {
        static ExtractZipAsync_Tests()
        {
            TestSetup.Initialize();
        }
        private static readonly string SongZipsPath = Path.Combine("Data", "SongZips");

        [TestMethod]
        public void ExtractZip_Normal_NotExistingDir()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "5d28-Normal";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            if (Directory.Exists(originalExtractPath))
                Directory.Delete(originalExtractPath, true);
            var zipResult= FileIO.ExtractZip(zipPath, originalExtractPath, false);
            
            Assert.IsTrue(zipResult.OutputDirectory.Equals(originalExtractPath));
            var extractedFolder = new DirectoryInfo(zipResult.OutputDirectory);
            Assert.IsTrue(File.Exists(zipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
            Directory.Delete(originalExtractPath, true);
            if (Directory.Exists(zipResult.OutputDirectory))
                Directory.Delete(zipResult.OutputDirectory, true);
        }

        [TestMethod]
        public void ExtractZip_NestedDirInZip()
        {
            string zipPath = Path.Combine(SongZipsPath, "5dd6-NestedDir.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Where(e => e.FullName.Equals(e.Name)).Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "5d28-Normal";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            if (Directory.Exists(originalExtractPath))
                Directory.Delete(originalExtractPath, true);
            var zipResult= FileIO.ExtractZip(zipPath, originalExtractPath, false);
            Assert.IsTrue(zipResult.OutputDirectory.Equals(originalExtractPath));
            var extractedFolder = new DirectoryInfo(zipResult.OutputDirectory);
            Assert.IsTrue(File.Exists(zipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                // Empty entry name = directory, not file
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
            Directory.Delete(originalExtractPath, true);
            if (Directory.Exists(zipResult.OutputDirectory))
                Directory.Delete(zipResult.OutputDirectory, true);
        }

        [TestMethod]
        public void ExtractZip_ExistingNoOverwrite()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "5d28-Existing";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            Directory.CreateDirectory(originalExtractPath);
            var zipResult= FileIO.ExtractZip(zipPath, originalExtractPath, false);
            var extractedFolder = new DirectoryInfo(zipResult.OutputDirectory);
            Assert.IsFalse(extractedFolder.Equals(originalExtractPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
            Directory.Delete(originalExtractPath, true);
            if (Directory.Exists(zipResult.OutputDirectory))
                Directory.Delete(zipResult.OutputDirectory, true);
        }

        [TestMethod]
        public void ExtractZip_PartialExtraction()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "5d28-Partial";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            var lockedFile = Path.Combine(originalExtractPath, entries.Last());
            Directory.CreateDirectory(originalExtractPath);
            using (var file = File.Create(lockedFile)) { }
            var fileLock = File.OpenRead(lockedFile);
            var zipResult = FileIO.ExtractZip(zipPath, originalExtractPath, true);
            fileLock.Dispose();
            File.Delete(lockedFile);
            var finalPath = zipResult.OutputDirectory;
            Assert.IsTrue(zipResult.ResultStatus == ZipExtractResultStatus.DestinationFailed);
            var extractedFolder = new DirectoryInfo(originalExtractPath);
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsFalse(extractedFiles.Contains(entry));
            }
            Directory.Delete(originalExtractPath, true);
            if (!string.IsNullOrEmpty(finalPath) && Directory.Exists(finalPath))
            {
                Directory.Delete(finalPath, true);
                Assert.Fail($"finalPath ({finalPath}) shouldn't exist.");
            }
        }

        [TestMethod]
        public void ExtractZip_Normal_CantDeleteZip()
        {
            string zipName = "5d28-LongEntry.zip";
            string zipPath = Path.Combine(SongZipsPath, zipName);
            string copiedZipPath = zipPath.Replace("Entry", "Entry-Copy");
            File.Copy(zipPath, copiedZipPath, true);
            string[] entries;
            using (var fs = new FileStream(copiedZipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "5d28-CantDeleteZip";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            ZipExtractResult zipResult = null;
            DirectoryInfo extractedFolder = null;
            using (var copiedZip = File.OpenRead(copiedZipPath))
            {
                zipResult = FileIO.ExtractZip(copiedZipPath, originalExtractPath, true);
                extractedFolder = new DirectoryInfo(zipResult.OutputDirectory);
            }
            Assert.IsTrue(File.Exists(copiedZipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }

            Directory.Delete(originalExtractPath, true);
            if (Directory.Exists(zipResult.OutputDirectory))
                Directory.Delete(zipResult.OutputDirectory, true);
        }

        [TestMethod]
        public void ExtractZip_DuplicateFileNames()
        {
            string zipPath = Path.Combine(SongZipsPath, "DuplicateFiles.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output";
            string songDir = "DuplicateFiles";
            string originalExtractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            if (Directory.Exists(originalExtractPath))
                Directory.Delete(originalExtractPath, true);
            var zipResult= FileIO.ExtractZip(zipPath, originalExtractPath, false);
            var extractedFolder = new DirectoryInfo(zipResult.OutputDirectory);
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
            Directory.Delete(originalExtractPath, true);
            Assert.AreEqual(originalExtractPath, zipResult.OutputDirectory);
            if (Directory.Exists(zipResult.OutputDirectory))
                Directory.Delete(zipResult.OutputDirectory, true);
        }

        [TestMethod]
        public void ExtractZip_DirectoryInaccessible()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string[] entries;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                entries = zipArchive.Entries.Select(e => e.Name).ToArray();
            }
            string songsPath = @"Output\Inaccessible";
            Directory.CreateDirectory(songsPath);
            var dInfo = new DirectoryInfo(songsPath);
            var security = dInfo.GetAccessControl();
            var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().User;
            var accessRule = new FileSystemAccessRule(currentUser,
                FileSystemRights.Write, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Deny);
            security.AddAccessRule(accessRule);
            dInfo.SetAccessControl(security);
            security.ModifyAccessRule(AccessControlModification.Remove, accessRule, out bool success);
            Assert.IsTrue(success);

            string songDir = "5d28-Inaccessible";
            string originalExtractPath = Path.Combine(songsPath, songDir);
            var zipResult= FileIO.ExtractZip(zipPath, originalExtractPath, false);
            Assert.IsTrue(zipResult.ExtractedFiles == null);
            dInfo.SetAccessControl(security);
            if (dInfo.Exists)
                dInfo.Delete(true);
            if (Directory.Exists(originalExtractPath))
            {
                Directory.Delete(originalExtractPath);
                Assert.Fail("Extraction directory should never have been created.");
            }
            if (zipResult.CreatedOutputDirectory)
            {
                if (Directory.Exists(zipResult.OutputDirectory))
                    Directory.Delete(originalExtractPath);
                Assert.Fail("Extraction directory should never have been created.");
            }

        }


    }
}
