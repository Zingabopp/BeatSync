using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class StatusUiConfig
        : ConfigBase
    {
        private const int DefaultTextRows = 5;
        private const int DefaultFadeTime = 10;
        private const float DefaultRowSpacing = .1f;
        private const float DefaultDistance = 2f;
        private const float DefaultHeight = 3.2f;
        private const int DefaultHorizontalAngle = 0;

        [JsonIgnore]
        private int? _textRows;
        [JsonIgnore]
        private int? _fadeTime;
        [JsonIgnore]
        private float? _rowSpacing;
        [JsonIgnore]
        private float? _distance;
        [JsonIgnore]
        private float? _height;
        [JsonIgnore]
        private int? _horizontalAngle;

        [JsonProperty("TextRows")]
        public int TextRows
        {
            get
            {
                if (_textRows == null)
                {
                    _textRows = DefaultTextRows;
                    SetConfigChanged();
                }
                return _textRows ?? DefaultTextRows;
            }
            set
            {
                int newAdjustedVal = value;
                if (value <= 0)
                {
                    newAdjustedVal = DefaultTextRows;
                    SetInvalidInputFixed();
                }
                if (_textRows == newAdjustedVal)
                    return;
                _textRows = newAdjustedVal;
                SetConfigChanged();
            }
        }

        [JsonProperty("FadeTime")]
        public int FadeTime
        {
            get
            {
                if (_fadeTime == null)
                {
                    _fadeTime = DefaultFadeTime;
                    SetConfigChanged();
                }
                return _fadeTime ?? DefaultFadeTime;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 0)
                {
                    newAdjustedVal = DefaultFadeTime;
                    SetInvalidInputFixed();
                }
                if (_fadeTime == newAdjustedVal)
                    return;
                _fadeTime = newAdjustedVal;
                SetConfigChanged();
            }
        }

        [JsonProperty("RowSpacing")]
        public float RowSpacing
        {
            get
            {
                if (_rowSpacing == null)
                {
                    _rowSpacing = DefaultRowSpacing;
                    SetConfigChanged();
                }
                return _rowSpacing ?? DefaultRowSpacing;
            }
            set
            {
                float newAdjustedVal = value;
                if (value <= 0)
                {
                    newAdjustedVal = DefaultRowSpacing;
                    SetInvalidInputFixed();
                }
                if (_rowSpacing == newAdjustedVal)
                    return;
                _rowSpacing = newAdjustedVal;
                SetConfigChanged();
            }
        }

        [JsonProperty("Distance")]
        public float Distance
        {
            get
            {
                if (_distance == null)
                {
                    _distance = DefaultDistance;
                    SetConfigChanged();
                }
                return _distance ?? DefaultDistance;
            }
            set
            {
                float newAdjustedVal = value;
                if (value <= 0)
                {
                    newAdjustedVal = DefaultDistance;
                    SetInvalidInputFixed();
                }
                if (_distance == newAdjustedVal)
                    return;
                _distance = newAdjustedVal;
                SetConfigChanged();
            }
        }

        [JsonProperty("Height")]
        public float Height
        {
            get
            {
                if (_height == null)
                {
                    _height = DefaultHeight;
                    SetConfigChanged();
                }
                return _height ?? DefaultHeight;
            }
            set
            {
                float newAdjustedVal = value;
                if (_height == newAdjustedVal)
                    return;
                _height = newAdjustedVal;
                SetConfigChanged();
            }
        }

        [JsonProperty("HorizontalAngle")]
        public int HorizontalAngle
        {
            get
            {
                if (_horizontalAngle == null)
                {
                    _horizontalAngle = DefaultHorizontalAngle;
                    SetConfigChanged();
                }
                return _horizontalAngle ?? DefaultHorizontalAngle;
            }
            set
            {
                int newAdjustedVal = value;
                //if (value <= 0)
                //{
                //    newAdjustedVal = DefaultHorizontalAngle;
                //    SetInvalidInputFixed();
                //}
                if (_horizontalAngle == newAdjustedVal)
                    return;
                _horizontalAngle = newAdjustedVal;
                SetConfigChanged();
            }
        }
        public override void FillDefaults()
        {
            var _ = TextRows;
            _ = FadeTime;
            var __ = RowSpacing;
            __ = Distance;
            __ = Height;
            __ = HorizontalAngle;
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is StatusUiConfig o)
            {
                bool result = TextRows == o.TextRows
                    && FadeTime == o.FadeTime
                    && RowSpacing == o.RowSpacing
                    && Distance == o.Distance
                    && Height == o.Height
                    && HorizontalAngle == o.HorizontalAngle;
                return result;
            }
            return false;
        }

        public override string ToString()
        {
            return $"StatusUiConfig: TextRows:{TextRows}, FadeTime:{FadeTime}, RowSpacing:{RowSpacing}, Distance:{Distance}, Height:{Height}, HorizontalAngle:{HorizontalAngle}";
        }
    }
}
