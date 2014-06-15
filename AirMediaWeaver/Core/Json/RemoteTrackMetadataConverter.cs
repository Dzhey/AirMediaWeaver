using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Platform.Data.Sql;
using Newtonsoft.Json;

namespace AirMedia.Core.Json
{
    public class RemoteTrackMetadataConverter : ConcreteTypeConverter<List<RemoteTrackMetadata>>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = serializer.Deserialize<List<RemoteTrackMetadata>>(reader);

            return obj.ConvertAll(input => (IRemoteTrackMetadata) input).ToList();
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}