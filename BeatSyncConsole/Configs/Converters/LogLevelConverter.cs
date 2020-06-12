using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncConsole.Utilities;
using BeatSyncLib.History;
using BeatSyncLib.Logging;
using Newtonsoft.Json;

namespace BeatSyncConsole.Configs.Converters
{
    internal class LogLevelConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(LogLevel) || t == typeof(LogLevel?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            string? value = serializer.Deserialize<string>(reader);
            if (value == null)
                return null;
            return (value.ToUpper()) switch
            {
                "DEBUG" =>    LogLevel.Debug,
                "0" =>        LogLevel.Debug,
                "INFO" =>     LogLevel.Info,
                "1" =>        LogLevel.Info,
                "WARN" =>     LogLevel.Warn,
                "2" =>        LogLevel.Warn,
                "CRITICAL" => LogLevel.Critical,
                "3" =>        LogLevel.Critical,
                "ERROR" =>    LogLevel.Error,
                "4" =>        LogLevel.Error,
                "DISABLED" => LogLevel.Disabled,
                "5" =>        LogLevel.Disabled,
                _ =>          LogLevel.Info,
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
            LogLevel installType = (LogLevel)value;
            serializer.Serialize(writer, installType.ToString());
            //throw new Exception("Cannot marshal type Category");
        }
    }
}