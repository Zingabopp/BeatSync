using BeatSaberMarkupLanguage.Attributes;
using BeatSync.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.UI.BSML
{
    internal class StatusUiSettings : ConfigUiBase
    {
        internal StatusUiConfig PreviousConfig { get; }
        internal StatusUiConfig Config { get; }
        public StatusUiSettings(StatusUiConfig previousConfig, StatusUiConfig newConfig)
        {
            PreviousConfig = previousConfig;
            Config = newConfig;
        }

        [UIValue("TextRowsChanged")]
        public bool TextRowsChanged { get { return Config.TextRows != PreviousConfig.TextRows; } }
        [UIValue("TextRows")]
        public int TextRows
        {
            get { return Config.TextRows; }
            set
            {
                if (Config.TextRows == value) return;
                Config.TextRows = value;
                NotifyPropertyChanged(nameof(TextRowsChanged));
            }
        }

        [UIValue("FadeTimeChanged")]
        public bool FadeTimeChanged { get { return Config.FadeTime != PreviousConfig.FadeTime; } }
        [UIValue("FadeTime")]
        public int FadeTime
        {
            get { return Config.FadeTime; }
            set
            {
                if (Config.FadeTime == value) return;
                Config.FadeTime = value;
                NotifyPropertyChanged(nameof(FadeTime));
            }
        }

        [UIValue("RowSpacingChanged")]
        public bool RowSpacingChanged { get { return Config.RowSpacing != PreviousConfig.RowSpacing; } }
        [UIValue("RowSpacing")]
        public float RowSpacing
        {
            get { return Config.RowSpacing; }
            set
            {
                if (Config.RowSpacing == value) return;
                Config.RowSpacing = value;
                NotifyPropertyChanged(nameof(RowSpacingChanged));
            }
        }

        [UIValue("DistanceChanged")]
        public bool DistanceChanged { get { return Config.Distance != PreviousConfig.Distance; } }
        [UIValue("Distance")]
        public float Distance
        {
            get { return Config.Distance; }
            set
            {
                if (Config.Distance == value) return;
                Config.Distance = value;
                NotifyPropertyChanged(nameof(DistanceChanged));
            }
        }

        [UIValue("HeightChanged")]
        public bool HeightChanged { get { return Config.Height != PreviousConfig.Height; } }
        [UIValue("Height")]
        public float Height
        {
            get { return Config.Height; }
            set
            {
                if (Config.Height == value) return;
                Config.Height = value;
                NotifyPropertyChanged(nameof(HeightChanged));
            }
        }

        [UIValue("HorizontalAngleChanged")]
        public bool HorizontalAngleChanged { get { return Config.HorizontalAngle != PreviousConfig.HorizontalAngle; } }
        [UIValue("HorizontalAngle")]
        public int HorizontalAngle
        {
            get { return Config.HorizontalAngle; }
            set
            {
                if (Config.HorizontalAngle == value) return;
                Config.HorizontalAngle = value;
                NotifyPropertyChanged(nameof(HorizontalAngleChanged));
            }
        }
    }
}
