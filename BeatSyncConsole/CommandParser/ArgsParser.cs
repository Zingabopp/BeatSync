using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeatSyncConsole.Utilities;
using CommandLine;

namespace BeatSyncConsole.CommandParser
{
    public class ArgsParser
    {
        private string ConfigDirectory { get; set; } = Path.Combine("%ASSEMBLYDIR%", "configs");
        private string? LogDirectory { get; set; } = Path.Combine("%ASSEMBLYDIR%", "logs");

        public readonly List<IEnumerable<Error>> ArgErrors = new List<IEnumerable<Error>>();
        public readonly List<string> ArgErrorMsgs = new List<string>();
        public readonly List<string> ArgDebugMsgs = new List<string>();

        public Options Options { get; private set; }
        public bool Successful { get; private set; }

        public ArgsParser()
        {
            Options = new Options() { ConfigDirectory = ConfigDirectory, LogDirectory = LogDirectory };
        }

        void HandleParseErrors(IEnumerable<Error> errors)
        {
            ArgErrors.Add(errors);
        }

        public void ParseArgs(string[] args)
        {
            Successful = false;
            ConfigDirectory = Path.Combine("%ASSEMBLYDIR%", "configs");
            LogDirectory = Path.Combine("%ASSEMBLYDIR%", "logs");
            ArgErrors.Clear();
            ArgErrorMsgs.Clear();
            ArgDebugMsgs.Clear();
            var parseResult = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseErrors);
            Successful = ArgErrors.Count == 0 && ArgErrorMsgs.Count == 0;
        }

        void RunOptions(Options options)
        {
            if(options == null)
            {
                Successful = false;
                Options = new Options() { ConfigDirectory = ConfigDirectory, LogDirectory = LogDirectory };
                return;
            }
            if (!string.IsNullOrWhiteSpace(options.LogDirectory))
            {
                string logPath = Paths.GetFullPath(options.LogDirectory, PathRoot.AssemblyDirectory);
                try
                {
                    Directory.CreateDirectory(logPath);
                    if (Directory.Exists(logPath))
                    {
                        LogDirectory = logPath;
                        ArgDebugMsgs.Add($"Set log directory to '{logPath}'");
                    }
                }
                catch (Exception ex)
                {
                    ArgErrorMsgs.Add($"Error setting Logging directory: {ex.Message}");
                }
            }
            if (!string.IsNullOrWhiteSpace(options.ConfigDirectory))
            {
                string configPath = Paths.GetFullPath(options.ConfigDirectory, PathRoot.AssemblyDirectory);
                try
                {
                    Directory.CreateDirectory(configPath);
                    if (Directory.Exists(configPath))
                    {
                        ConfigDirectory = configPath;
                        ArgDebugMsgs.Add($"Set config directory to '{configPath}'");
                    }
                }
                catch (Exception ex)
                {
                    ArgErrorMsgs.Add($"Error setting Config directory: {ex.Message}");
                }
            }
            options.LogDirectory = LogDirectory;
            options.ConfigDirectory = ConfigDirectory;
            Options = options;
        }
    }
}
