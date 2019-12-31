using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSyncLib.Configs
{
    internal class PlaylistStyleConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(PlaylistStyle) || t == typeof(PlaylistStyle?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value.ToLower())
            {
                case "append":
                    return PlaylistStyle.Append;
                case "replace":
                    return PlaylistStyle.Replace;
                case "0":
                    return PlaylistStyle.Append;
                case "1":
                    return PlaylistStyle.Replace;
                default:
                    return null;
            }
            //throw new Exception("Cannot unmarshal type PlaylistStyle");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var playlistStyle = (PlaylistStyle)value;
            switch (playlistStyle)
            {
                case PlaylistStyle.Append:
                    serializer.Serialize(writer, "Append");
                    return;
                case PlaylistStyle.Replace:
                    serializer.Serialize(writer, "Replace");
                    return;
                default:
                    serializer.Serialize(writer, null);
                    return;
            }
            //throw new Exception("Cannot marshal type Category");
        }
    }
}
