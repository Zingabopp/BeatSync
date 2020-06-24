using BeatSyncLib.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class BeatSyncModConfig : ConfigBase
    {
        public virtual SyncIntervalConfig TimeBetweenSyncs { get; set; } = new SyncIntervalConfig(0, 1);
        [JsonIgnore]
        public BeatSyncConfig BeatSyncConfig { get; set; }


        public static BeatSyncModConfig GetDefaultConfig()
        {
            return new BeatSyncModConfig();
        }

        public override void FillDefaults()
        {

        }

        public override bool ConfigMatches(ConfigBase other)
        {
            return false;
        }
    }
}
