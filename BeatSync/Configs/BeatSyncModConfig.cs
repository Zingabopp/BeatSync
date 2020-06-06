using BeatSyncLib.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class BeatSyncModConfig
    {
        public virtual SyncIntervalConfig TimeBetweenSyncs { get; set; }
    }
}
