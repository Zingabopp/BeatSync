using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncConsole.Utilities;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace BeatSyncConsoleTests
{
    [TestClass]
    public class PathTests
    {
        [TestMethod]
        public void OperatingSystemType()
        {
            Console.WriteLine(RuntimeInformation.OSDescription);
        }

        [TestMethod]
        public void AssemblyLocation()
        {
            Console.WriteLine(Paths.AssemblyDirectory);
        }

        [TestMethod]
        public void WorkingDirectory()
        {
            Console.WriteLine(Paths.WorkingDirectory);
        }

        [TestMethod]
        public void TempDirectory()
        {
            Console.WriteLine(Paths.TempDirectory);
        }

        [TestMethod]
        public void RootTest()
        {
            string rootPath = Path.DirectorySeparatorChar.ToString();
            Assert.IsTrue(Directory.Exists(rootPath));
            Console.WriteLine(string.Join('\n', Directory.GetDirectories(rootPath)));
        }
    }
}
