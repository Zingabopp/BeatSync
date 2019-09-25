using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync
{
    public class BuildManifestInfo : ITask
    {
        [Output]
        public string PluginVersion { get; set; }
        [Output]
        public string GameVersion { get; set; }

        public IBuildEngine BuildEngine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ITaskHost HostObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Execute()
        {
            try
            {
                JToken manifestJson = JsonConvert.DeserializeObject<JToken>(File.ReadAllText("manifest.json"));
                string version = manifestJson["version"]?.Value<string>();
                string gameVersion = manifestJson["gameVersion"]?.Value<string>();
                PluginVersion = string.IsNullOrEmpty(version) ? "E.R.R" : version;
                GameVersion = string.IsNullOrEmpty(gameVersion) ? "E.R.R" : gameVersion;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}
