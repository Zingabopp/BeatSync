using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.CommandParser
{
    public class Options
    {
        [Option('c', "ConfigDirectory", Required = false, HelpText = "Sets the directory BeatSyncConsole.json is read/stored.")]
        public string? ConfigDirectory { get; set; }

        [Option('L', "LogDirectory", Required = false, HelpText = "Sets the directory log files are stored.")]
        public string? LogDirectory { get; set; }
    }
}
