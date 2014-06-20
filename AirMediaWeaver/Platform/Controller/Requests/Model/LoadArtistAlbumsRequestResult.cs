using System.Collections.Generic;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.UI.Library.AlbumList;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Model
{
    public class LoadArtistAlbumsRequestResult : CachedLoadRequestResult<List<AlbumListEntry>>
    {
        public LoadArtistAlbumsRequestResult(int resultCode, List<AlbumListEntry> resultData) : base(resultCode, resultData)
        {
        }

        [Newtonsoft.Json.JsonIgnore]
        public KeyValuePair<long, Bitmap>[] AlbumArts { get; set; }

        [Newtonsoft.Json.JsonIgnore] 
        private bool _isDisposed;

        public LoadArtistAlbumsRequestResult()
        {
        }

        public LoadArtistAlbumsRequestResult(int resultCode)
        {
            ResultCode = resultCode;
        }

        protected override void ApplyDeserializedParams(CachedLoadRequestResult<List<AlbumListEntry>> previousResult)
        {
            base.ApplyDeserializedParams(previousResult);

            AlbumArts = new KeyValuePair<long, Bitmap>[0];
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (AlbumArts != null)
                {
                    foreach (var entry in AlbumArts)
                    {
                        entry.Value.Dispose();
                    }
                }
                AlbumArts = null;
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}