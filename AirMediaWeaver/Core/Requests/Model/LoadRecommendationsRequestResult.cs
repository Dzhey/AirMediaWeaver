using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Json;
using Newtonsoft.Json;

namespace AirMedia.Core.Requests.Model
{
    public class LoadRecommendationsRequestResult : CachedLoadRequestResult<List<IRemoteTrackMetadata>>
    {
        [JsonConverter(typeof(RemoteTrackMetadataConverter))]
        public new List<IRemoteTrackMetadata> Data { get; set; }

        public LoadRecommendationsRequestResult()
        {
        }

        public LoadRecommendationsRequestResult(int resultCode, List<IRemoteTrackMetadata> resultData)
            : base(resultCode, resultData)
        {
            Data = resultData;
        }

        protected override void ApplyDeserializedParams(CachedLoadRequestResult<List<IRemoteTrackMetadata>> previousResult)
        {
            base.ApplyDeserializedParams(previousResult);

            Data = ((LoadRecommendationsRequestResult) previousResult).Data;
        }
    }
}