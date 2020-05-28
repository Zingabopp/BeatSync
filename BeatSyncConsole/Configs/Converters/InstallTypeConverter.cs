using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncConsole.Utilities;
using BeatSyncLib.History;
using Newtonsoft.Json;

namespace BeatSyncConsole.Configs.Converters
{
    internal class InstallTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(InstallType) || t == typeof(InstallType?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            string? value = serializer.Deserialize<string>(reader);
            if (value == null)
                return null;
            return (value.ToUpper()) switch
            {
                "CUSTOM" => InstallType.Custom,
                "0" => InstallType.Custom,
                "STEAM" => InstallType.Steam,
                "1" => InstallType.Steam,
                "OCULUS" => InstallType.Oculus,
                "2" => InstallType.Oculus,
                _ => InstallType.Custom,
            };
            //throw new Exception("Cannot unmarshal type PlaylistStyle");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            InstallType installType = (InstallType)value;
            serializer.Serialize(writer, installType.ToString());
            //throw new Exception("Cannot marshal type Category");
        }
    }
}
