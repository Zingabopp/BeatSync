using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace BeatSyncTests
{
    //[TestClass]
    public class UnitTest1
    {
        //[TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("Test");
        }

        //[TestMethod]
        //public void GetStuff()
        //{
        //    string PluginVersion;
        //    string GameVersion;

        //    try
        //    {
        //        string manifestFile = @"manifest.json";
        //        string assemblyFile = @"Properties\AssemblyInfo.cs";
        //        var assemblyVersionRegex = new Regex(@"^\[assembly: AssemblyVersion\(\""?(.*)\""\)\]",
        //            RegexOptions.Multiline | RegexOptions.Compiled);
        //        if (!File.Exists(manifestFile))
        //        {
        //            throw new FileNotFoundException("Could not find manifest: " + Path.GetFullPath(manifestFile));
        //        }
        //        if (!File.Exists(assemblyFile))
        //        {
        //            throw new FileNotFoundException("Could not find AssemblyInfo: " + Path.GetFullPath(assemblyFile));
        //        }
        //        JToken manifestJson = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(manifestFile));
        //        if (manifestJson["version"] != null)
        //        {
        //            string version = manifestJson["version"].Value<string>();
        //            PluginVersion = string.IsNullOrEmpty(version) ? "E.R.R" : version;
        //        }
        //        if (manifestJson["gameVersion"] != null)
        //        {
        //            string gameVersion = manifestJson["gameVersion"].Value<string>();
        //            GameVersion = string.IsNullOrEmpty(gameVersion) ? "E.R.R" : gameVersion;
        //        }

        //        var assemblyText = File.ReadAllText(assemblyFile);
        //        var assemblyVersion = assemblyVersionRegex.Match(assemblyText).Groups[1].Value;
        //        if (!assemblyVersion.Equals(PluginVersion))
        //        {
        //            Logger.LogWarning("PluginVersion {0} does not match AssemblyVersion {1}", PluginVersion, assemblyVersion);
        //        }


        //        return;// true;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //        Log.LogErrorFromException(ex);
        //        return;// false;
        //    }
        //}
    }
}
