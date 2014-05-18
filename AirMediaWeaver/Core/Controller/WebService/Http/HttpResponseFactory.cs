using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using Newtonsoft.Json;

namespace AirMedia.Core.Controller.WebService.Http
{
    public class HttpResponseFactory
    {
        public static readonly string LogTag = typeof (HttpResponseFactory).Name;

        public const int HttpResponseNone = 0;
        public const int HttpResponsePublishedTracks = 1;

        public static readonly IDictionary<int, Type> ResponseTypes;

        static HttpResponseFactory()
        {
            ResponseTypes = new Dictionary<int, Type>();
            ResponseTypes.Add(HttpResponsePublishedTracks, typeof(HttpResponsePublishedTracks));
        }

        public static HttpResponseModel CreateResponse(int errorCode, 
            string reasonPhrase = null, HttpResponse response = null)
        {
            var model = new HttpResponseModel();

            model.ErrorCode = errorCode;
            model.ReasonPhrase = reasonPhrase;

            if (response == null) return model;

            model.ResponseType = response.ResponseType;
            model.ResponseData = JsonConvert.SerializeObject(response);

            return model;
        }

        public static HttpResponse CreateResponse(HttpResponseModel model)
        {
            if (model == null || model.ResponseData == null) return null;

            if (model.ResponseType == HttpResponseNone) return null;

            Type responseType;
            if (ResponseTypes.TryGetValue(model.ResponseType, out responseType) == false)
            {
                AmwLog.Error(LogTag, string.Format("can't parse response model: undefined" +
                                                    " response type \"{0}\"", model.ResponseType));

                return null;
            }

            return (HttpResponse) JsonConvert.DeserializeObject(model.ResponseData, responseType);
        }

        public static string PackResponse(HttpResponseModel response)
        {
            return JsonConvert.SerializeObject(response);
        }

        public static HttpResponseModel UnpackResponse(string data)
        {
            if (data == null) return null;

            return (HttpResponseModel) JsonConvert.DeserializeObject(data, typeof (HttpResponseModel));
        }
    }
}