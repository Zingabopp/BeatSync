using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncConsole.Utilities
{
    public enum OperatingSystem
    {
        Unknown,
        Windows,
        Linux,
        OSX,
        FreeBSD
    }
    public static class Paths
    {
        private static bool OsDetected = false;
        private static OperatingSystem _operatingSystem;
        public static OperatingSystem OperatingSystem
        {
            get
            {
                if (OsDetected)
                    return _operatingSystem;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _operatingSystem = OperatingSystem.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    _operatingSystem = OperatingSystem.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    _operatingSystem = OperatingSystem.OSX;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    _operatingSystem = OperatingSystem.FreeBSD;
                else
                {
                    Logger.log?.Warn($"Operating system not recognized: {RuntimeInformation.OSDescription}");
                    _operatingSystem = OperatingSystem.Unknown;
                }
                OsDetected = true;
                return _operatingSystem;
            }
        }

        private static string? _assemblyDirectory;
        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                    _assemblyDirectory = Assembly.GetExecutingAssembly().Location;
                return _assemblyDirectory;
            }
        }

        public const string Path_CustomLevels = @"Beat Saber_Data\CustomLevels";
        public const string Path_Playlists = @"Playlists";
        public const string Path_History = @"UserData\BeatSyncHistory.json";
        public static BeatSaberInstallLocation ToSongLocation(this BeatSaberInstall install)
        {
            return new BeatSaberInstallLocation(install.InstallPath);
        }

        public static string ReplaceWorkingDirectory(string fullPath) => fullPath.Replace(Directory.GetCurrentDirectory(), ".");

        public static string GetFullPath(string path)
        {
            string fullPathStr = path;

            return fullPathStr;
        }
    }
}
