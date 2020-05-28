using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncLib.Configs
{
    public class SyncIntervalConfig
       : ConfigBase
    {
        [JsonIgnore]
        private int? _hours;
        [JsonIgnore]
        private int? _minutes;

        public SyncIntervalConfig()
        { }

        public SyncIntervalConfig(int hours, int minutes)
        {
            Hours = hours;
            Minutes = minutes;
            ResetConfigChanged();
        }

        [JsonProperty("Hours")]
        public int Hours
        {
            get
            {
                if (_hours == null)
                {
                    _hours = 0;
                    SetConfigChanged();
                }
                return _hours ?? 0;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 0)
                {
                    newAdjustedVal = 0;
                    SetInvalidInputFixed();
                }
                if (_hours == newAdjustedVal)
                    return;
                _hours = newAdjustedVal;
                SetConfigChanged();
            }
        }
        [JsonProperty("Minutes")]
        public int Minutes
        {
            get
            {
                if (_minutes == null)
                {
                    _minutes = 10;
                    SetConfigChanged();
                }
                return _minutes ?? 10;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 0)
                {
                    newAdjustedVal = 10;
                    SetInvalidInputFixed();
                }
                if (_minutes == newAdjustedVal)
                    return;
                _minutes = newAdjustedVal;
                SetConfigChanged();
            }
        }

        public override string ToString()
        {
            return $"{(Hours == 1 ? "1 hour" : $"{Hours} hours")} {(Minutes == 1 ? "1 minute" : $"{Minutes} minutes")}";
        }

        public override void FillDefaults()
        {
            var _ = Hours;
            var __ = Minutes;
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            return this.Equals(other);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is SyncIntervalConfig other)
            {
                if (Hours != other.Hours)
                    return false;
                if (Minutes != other.Minutes)
                    return false;
            }
            else
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash ^= Hours * 3 + 197;
            hash ^= Minutes * 103 + 23;
            hash *= 181;
            return hash;
        }
    }
}
