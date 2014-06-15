using System;
using AirMedia.Core.Requests.Interfaces;
using Newtonsoft.Json;

namespace AirMedia.Core.Requests.Model
{
    public class CachedLoadRequestResult<TData> : LoadRequestResult<TData>, ICachedRequestResult
    {
        public CachedLoadRequestResult()
        {
        }

        public CachedLoadRequestResult(int resultCode, TData resultData)
            : base(resultCode, resultData)
        {
        }

        internal CachedLoadRequestResult(int resultCode, TData resultData, Exception risenException)
            : base(resultCode, resultData, risenException)
        {
            Data = resultData;
        }

        public virtual byte[] Serialize()
        {
            return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

        public virtual void Deserialize(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var obj = JsonConvert.DeserializeObject(json, GetType());
            ApplyDeserializedParams((CachedLoadRequestResult<TData>) obj);
        }

        protected virtual void ApplyDeserializedParams(CachedLoadRequestResult<TData> previousResult)
        {
            if (previousResult.HasResultCode)
            {
                ResultCode = previousResult.ResultCode;
            }
            RisenException = previousResult.RisenException;
            Data = previousResult.Data;
        }
    }
}