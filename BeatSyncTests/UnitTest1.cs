using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace BeatSyncTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("Test");
        }

        [TestMethod]
        public void GetStuff()
        {
            JToken thing = JsonConvert.DeserializeObject<JToken>(File.ReadAllText("manifest.json"));
            string gameVersion = thing["gameVersion"].Value<string>();
            string version = thing["version"].Value<string>();
            Assert.AreEqual("1.3.0", gameVersion);
            Assert.AreEqual("0.2.4", version);
        }
    }
}
