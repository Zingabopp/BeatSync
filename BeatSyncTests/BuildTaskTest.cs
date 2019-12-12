using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.Build.Framework;
using System.Diagnostics;

namespace BeatSyncTests
{
    [TestClass]
    public class BuildTaskTest
    {
        [TestMethod]
        public void TestMethod1()
        {

        }
        private string ProjectDir = @"C:\Users\Jared\source\repos\BeatSync\BeatSync";
        private string CommitShortHash;

        [TestMethod]
        public bool GetCommitHash()
        {
            CommitShortHash = "local";
            try
            {
                Process process = new Process();
                string arg = "rev-parse HEAD";
                process.StartInfo = new ProcessStartInfo("git", arg);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = ProjectDir;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                var outText = process.StandardOutput.ReadToEnd();
                if (outText.Length >= 7)
                    CommitShortHash = outText.Substring(0, 7);
                //return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return true;
            }
            Assert.AreEqual(7, CommitShortHash.Length);
            Console.WriteLine($"CommitShortHash: {CommitShortHash}");
            return true;
        }

        public string PluginVersion;
        public string AssemblyVersion;
        public string GameVersion;
        [TestMethod]
        public bool GetManifestInfo()
        {
            try
            {
                string manifestFile = "manifest.json";
                string manifest_gameVerStart = "\"gameVersion\"";
                string manifest_versionStart = "\"version\"";
                string manifest_gameVerLine = null;
                string manifest_versionLine = null;
                string assemblyFile = "Properties\\AssemblyInfo.cs";
                string startString = "[assembly: AssemblyVersion(\"";
                string secondStartString = "[assembly: AssemblyFileVersion(\"";
                string assemblyFileVersion = null;
                string firstLineStr = null;
                string endLineStr = null;
                bool badParse = false;
                int startLine = 1;
                int endLine = 0;
                int startColumn = 0;
                int endColumn = 0;
                if (!File.Exists(manifestFile))
                {
                    throw new FileNotFoundException("Could not find manifest: " + Path.GetFullPath(manifestFile));
                }
                if (!File.Exists(assemblyFile))
                {
                    throw new FileNotFoundException("Could not find AssemblyInfo: " + Path.GetFullPath(assemblyFile));
                }
                string line;
                using (StreamReader manifestStream = new StreamReader(manifestFile))
                {
                    while ((line = manifestStream.ReadLine()) != null && (manifest_versionLine == null || manifest_gameVerLine == null))
                    {
                        line = line.Trim();
                        if (line.StartsWith(manifest_gameVerStart))
                        {
                            manifest_gameVerLine = line;
                        }
                        else if (line.StartsWith(manifest_versionStart))
                        {
                            manifest_versionLine = line;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(manifest_versionLine))
                {
                    PluginVersion = manifest_versionLine.Substring(manifest_versionStart.Length).Replace(":","").Replace("\"", "").Trim();
                }
                else
                    PluginVersion = "E.R.R";

                if (!string.IsNullOrEmpty(manifest_gameVerLine))
                {
                    GameVersion = manifest_gameVerLine.Substring(manifest_gameVerStart.Length).Replace(":", "").Replace("\"", "").Trim();
                }
                else
                    GameVersion = "E.R.R";

                line = null;
                using (StreamReader assemblyStream = new StreamReader(assemblyFile))
                {
                    while ((line = assemblyStream.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith(startString))
                        {
                            firstLineStr = line;
                            break;
                        }
                        startLine++;
                        endLine = startLine + 1;
                    }
                    while ((line = assemblyStream.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith(secondStartString))
                        {
                            endLineStr = line;
                            break;
                        }
                        endLine++;
                    }
                }
                if (!string.IsNullOrEmpty(firstLineStr))
                {
                    startColumn = firstLineStr.IndexOf('"') + 1;
                    endColumn = firstLineStr.LastIndexOf('"');
                    if (startColumn > 0 && endColumn > 0)
                        AssemblyVersion = firstLineStr.Substring(startColumn, endColumn - startColumn);
                    else
                        badParse = true;
                }
                else
                    badParse = true;
                if (badParse)
                {
                    Log.LogError("Build", "BSMOD03", "", assemblyFile, 0, 0, 0, 0, "Unable to parse the AssemblyVersion from {0}", assemblyFile);
                    badParse = false;
                }

                if (AssemblyVersion != PluginVersion)
                {
                    Log.LogError("Build", "BSMOD01", "", assemblyFile, startLine, startColumn + 1, startLine, endColumn + 1, "PluginVersion {0} in manifest.json does not match AssemblyVersion {1} in AssemblyInfo.cs", PluginVersion, AssemblyVersion, assemblyFile);
                    Log.LogMessage(MessageImportance.High, "PluginVersion {0} does not match AssemblyVersion {1}", PluginVersion, AssemblyVersion);
                }
                if (!string.IsNullOrEmpty(endLineStr))
                {
                    startColumn = endLineStr.IndexOf('"') + 1;
                    endColumn = endLineStr.LastIndexOf('"');
                    if (startColumn > 0 && endColumn > 0)
                    {
                        assemblyFileVersion = endLineStr.Substring(startColumn, endColumn - startColumn);
                        if (AssemblyVersion != assemblyFileVersion)
                            Log.LogWarning("Build", "BSMOD02", "", assemblyFile, endLine, startColumn + 1, endLine, endColumn + 1, "AssemblyVersion {0} does not match AssemblyFileVersion {1} in AssemblyInfo.cs", AssemblyVersion, assemblyFileVersion);

                    }
                    else
                    {
                        Log.LogError("Build", "BSMOD03", "", assemblyFile, 0, 0, 0, 0, "Unable to parse the AssemblyFileVersion from {0}", assemblyFile);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw;
                Log.LogErrorFromException(ex);
                return false;
            }

        }

        public static class Log
        {
            public static void LogErrorFromException(Exception ex)
            {
                Console.Write($"{ex.Message}");
            }
            public static void LogMessage(MessageImportance importance, string message, params object[] messageArgs)
            {
                Console.WriteLine($"{importance.ToString()}: {message}", messageArgs);
            }
            public static void LogError(string subcategory, string errorCode, string helpKeyword, string file,
                int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber, string message, params object[] messageArgs)
            {
                Console.WriteLine($"ERROR: {subcategory}.{errorCode} | {file}({lineNumber}-{endLineNumber}:{columnNumber}-{endColumnNumber}): {message}", messageArgs);
            }
            public static void LogWarning(string subcategory, string warningCode, string helpKeyword, string file,
                int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber, string message, params object[] messageArgs)
            {
                Console.WriteLine($"Warning: {subcategory}.{warningCode} | {file}({lineNumber}-{endLineNumber}:{columnNumber}-{endColumnNumber}): {message}", messageArgs);
            }
        }
    }
}