using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncConsole.Utilities;

namespace BeatSyncConsoleTests
{
    [TestClass]
    public class PathTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(OperatingSystem.Windows, Paths.OperatingSystem);
        }
    }
}
