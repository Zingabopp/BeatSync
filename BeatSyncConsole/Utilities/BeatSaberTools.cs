#if !NOREGISTRY
using Microsoft.Win32;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeatSyncConsole.Utilities
{
    public static class BeatSaberTools
    {
        // Using Path.Combine makes it safe for regions that don't use '\' as a directory separator?
        private static readonly string STEAM_REG_KEY = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Uninstall", "Steam App 620980");
        //private const string STEAM_REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980";
        private static readonly string OCULUS_LM_KEY = Path.Combine("SOFTWARE", "WOW6432Node", "Oculus VR, LLC", "Oculus", "Config");
        private static readonly string OCULUS_CU_KEY = Path.Combine("SOFTWARE", "Oculus VR, LLC", "Oculus", "Libraries");
        //private const string OCULUS_REG_KEY = @"SOFTWARE\WOW6432Node\Oculus VR, LLC\Oculus\Config";
#if !NOREGISTRY
        public static BeatSaberInstall[] GetBeatSaberPathsFromRegistry()
        {
            List<BeatSaberInstall>? installList = new List<BeatSaberInstall>();
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))// Doesn't work in 32 bit mode without this
            {
                using (RegistryKey? steamKey = hklm?.OpenSubKey(STEAM_REG_KEY))
                {
                    string? path = (string?)steamKey?.GetValue("InstallLocation", string.Empty);
                    if (path != null && IsBeatSaberDirectory(path))
                        installList.Add(new BeatSaberInstall(path, InstallType.Steam));
                }
                string[] oculusLibraries = GetOculusLibraryPaths();
                foreach (string? library in oculusLibraries)
                {
                    string? matchedLocation = FindBeatSaberInOculusLibrary(library);
                    if (!string.IsNullOrEmpty(matchedLocation))
                        installList.Add(new BeatSaberInstall(matchedLocation, InstallType.Oculus));
                }
            }
            return installList.ToArray();
        }
#endif
        public static string? FindBeatSaberInOculusLibrary(string oculusLibraryPath)
        {
            if (oculusLibraryPath == null) return null;
            string possibleLocation = Path.Combine(oculusLibraryPath, "hyperbolic-magnetism-beat-saber");
            string? matchedLocation = null;
            if (Directory.Exists(possibleLocation))
            {
                if (IsBeatSaberDirectory(possibleLocation))
                    return possibleLocation;
            }
            else
            {
                string softwareFolder = Path.Combine(oculusLibraryPath, "Software");
                if (Directory.Exists(softwareFolder))
                    matchedLocation = FindBeatSaberInOculusLibrary(softwareFolder);
            }
            return matchedLocation;
        }
#if !NOREGISTRY
        public static string[] GetOculusLibraryPaths()
        {
            List<string> paths = new List<string>();
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)) // Doesn't work in 32 bit mode without this
            {
                using RegistryKey? oculusKey = hklm?.OpenSubKey(OCULUS_LM_KEY);
                string? path = (string?)oculusKey?.GetValue("InitialAppLibrary", string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }
            using (RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)) // Doesn't work in 32 bit mode without this
            {
                using RegistryKey? oculusKey = hkcu?.OpenSubKey(OCULUS_CU_KEY);
                if (oculusKey != null && oculusKey.SubKeyCount > 0)
                {
                    foreach (string? libraryKeyName in oculusKey.GetSubKeyNames())
                    {
                        using RegistryKey? library = oculusKey.OpenSubKey(libraryKeyName);
                        string? path = (string?)library?.GetValue("OriginalPath", string.Empty);
                        if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                            paths.Add(path);
                    }
                }
            }
            return paths.ToArray();
        }
#endif
        public static readonly char[] IllegalCharacters = new char[]
            {
                '<', '>', ':', '/', '\\', '|', '?', '*', '"',
                '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
                '\u0008', '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u000e', '\u000d',
                '\u000f', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016',
                '\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d', '\u001f',
            };
        /// <summary>
        /// Attempts to get the Beat Saber game version from the given install directory. Returns null if it fails.
        /// </summary>
        /// <remarks>
        /// Uses the implementation from https://github.com/Assistant/ModAssistant
        /// </remarks>
        /// <param name="gameDir"></param>
        /// <returns></returns>
        public static string? GetVersion(string gameDir)
        {
            string filename = Path.Combine(gameDir, "Beat Saber_Data", "globalgamemanagers");
            if (!File.Exists(filename))
                return null;
            try
            {
                using FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                byte[] file = File.ReadAllBytes(filename);
                byte[] bytes = new byte[16];

                fs.Read(file, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                int index = Encoding.Default.GetString(file).IndexOf("public.app-category.games") + 136;

                Array.Copy(file, index, bytes, 0, 16);
                string version = Encoding.Default.GetString(bytes).Trim(IllegalCharacters);

                return version;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return null;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public static bool IsBeatSaberDirectory(string? path)
        {
            if (string.IsNullOrEmpty(path?.Trim()))
                return false;
            DirectoryInfo? bsDir = null;
            try
            {
                bsDir = new DirectoryInfo(path);
                if (bsDir.Exists)
                {
                    FileInfo[]? files = bsDir.GetFiles("Beat Saber.exe");
                    return files.Count() > 0;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types

            return false;
        }
    }
}
