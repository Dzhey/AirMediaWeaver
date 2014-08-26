using System.Collections.Generic;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Model
{
    public class LoadLocalAlbumsRequestResult : CachedLoadRequestResult<List<AlbumGridItem>>
    {
        public LoadLocalAlbumsRequestResult(int resultCode, List<AlbumGridItem> resultData)
            : base(resultCode, resultData)
        {
        }

        [Newtonsoft.Json.JsonIgnore]
        public KeyValuePair<long, Bitmap>[] AlbumArts { get; set; }

        [Newtonsoft.Json.JsonIgnore] 
        private bool _isDisposed;

        public LoadLocalAlbumsRequestResult()
        {
        }

        public LoadLocalAlbumsRequestResult(int resultCode)
        {
            ResultCode = resultCode;
        }

        protected override void ApplyDeserializedParams(CachedLoadRequestResult<List<AlbumGridItem>> previousResult)
        {
            base.ApplyDeserializedParams(previousResult);

            AlbumArts = new KeyValuePair<long, Bitmap>[0];
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                AlbumArts = null;
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}