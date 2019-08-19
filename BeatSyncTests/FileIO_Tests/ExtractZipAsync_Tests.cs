using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using BeatSync.Utilities;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;

namespace BeatSyncTests.FileIO_Tests
{
    [TestClass]
    public class ExtractZipAsync_Tests
    {
        private static readonly string SongZipsPath = Path.Combine("Data", "SongZips");

        [TestMethod]
        public void ExtractZip_Normal()
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
            string extractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            var extractedFolder = new DirectoryInfo(FileIO.ExtractZipAsync(zipPath, extractPath, false, false).Result);
            Assert.IsTrue(File.Exists(zipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
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
            string songDir = "5d28-Normal";
            string extractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            var lockedFile = Path.Combine(extractPath, entries.Last());
            Directory.CreateDirectory(extractPath);
            using (var file = File.Create(lockedFile)) { }
            var fileLock = File.OpenRead(lockedFile);
            var finalPath = FileIO.ExtractZipAsync(zipPath, extractPath, false, true).Result;
            Assert.IsTrue(finalPath == null);
            var extractedFolder = new DirectoryInfo(extractPath);
            
            fileLock.Dispose();
            File.Delete(lockedFile);
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsFalse(extractedFiles.Contains(entry));
            }
        }

        [TestMethod]
        public void ExtractZip_Normal_DeleteZip()
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
            string songDir = "5d28-Copied";
            string extractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            var extractedFolder = new DirectoryInfo(FileIO.ExtractZipAsync(copiedZipPath, extractPath, true, false).Result);
            Assert.IsFalse(File.Exists(copiedZipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
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
            string songDir = "5d28-Copied";
            string extractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            DirectoryInfo extractedFolder = null;
            using (var copiedZip = File.OpenRead(copiedZipPath))
            {
                extractedFolder = new DirectoryInfo(FileIO.ExtractZipAsync(copiedZipPath, extractPath, true, false).Result);
            }
            Assert.IsTrue(File.Exists(copiedZipPath));
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
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
            string extractPath = Path.GetFullPath(Path.Combine(songsPath, songDir));
            var extractedFolder = new DirectoryInfo(FileIO.ExtractZipAsync(zipPath, extractPath, false, false).Result);
            var extractedFiles = extractedFolder.GetFiles().Select(f => f.Name);
            foreach (var entry in entries)
            {
                Assert.IsTrue(extractedFiles.Contains(entry));
            }
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
            security.AddAccessRule(new FileSystemAccessRule(currentUser,
                FileSystemRights.Write, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Deny));
            dInfo.SetAccessControl(security);
            string songDir = "5d28-Inaccessible";
            string extractPath = Path.Combine(songsPath, songDir);
            extractPath = FileIO.ExtractZipAsync(zipPath, extractPath, false, false).Result;
            Assert.IsTrue(extractPath == null);
        }


    }
}
