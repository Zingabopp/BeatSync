using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncLib.History;
using Newtonsoft.Json;

namespace BeatSyncLib.Configs.Converters
{
    internal class HistoryFlagConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(HistoryFlag) || t == typeof(HistoryFlag?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            string? value = serializer.Deserialize<string>(reader);
            if (value == null)
                return null;
            return (value.ToUpper()) switch
            {
                "NONE" => HistoryFlag.None,
                "0" => HistoryFlag.None,
                "DOWNLOADED" => HistoryFlag.Downloaded,
                "1" => HistoryFlag.Downloaded,
                "DELETED" => HistoryFlag.Deleted,
                "2" => HistoryFlag.Deleted,
                "MISSING" => HistoryFlag.Missing,
                "3" => HistoryFlag.Missing,
                "PREEXISTING" => HistoryFlag.PreExisting,
                "4" => HistoryFlag.PreExisting,
                "ERROR" => HistoryFlag.Error,
                "5" => HistoryFlag.Error,
                "BEATSAVERNOTFOUND" => HistoryFlag.BeatSaverNotFound,
                "404" => HistoryFlag.BeatSaverNotFound,
                _ => null,
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
            HistoryFlag historyFlag = (HistoryFlag)value;
            serializer.Serialize(writer, historyFlag.ToString());
            //throw new Exception("Cannot marshal type Category");
        }
    }
}
