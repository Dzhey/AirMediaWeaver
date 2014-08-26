using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.Logger;
using AirMedia.Platform.UI.Base;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public class AlbumsGridWorker : BaseContextualRequestWorker, IAlbumListContentWorker
    {
        public static readonly string LogTag = typeof(AlbumsGridWorker).Name;

        public IAlbumListAdapter Adapter { get { return _adapter; } }

        public bool IsAlbumArtsLoaderEnabled
        {
            get { return Adapter.IsAlbumArtsLoaderEnabled; }
            set { Adapter.IsAlbumArtsLoaderEnabled = value; }
        }

        private readonly IAlbumListAdapter _adapter;
        private bool _isDisposed;
        private readonly IAlbumListContentWorkerCallbacks _callbacks;

        public AlbumsGridWorker(IAlbumListContentWorkerCallbacks callbacks) : base(callbacks)
        {
            _adapter = new AlbumListGridAdapter();
            _callbacks = callbacks;
        }

        protected override AndroidRequestFactory CreateRequestFactory(RequestResultListener listener)
        {
            var factory = RequestFactory.Init(typeof(AndroidLoadLocalArtistAlbumsRequest));

            return (AndroidRequestFactory) AndroidRequestFactory.Init(factory, listener)
                                                                .SetParallel(true)
                                                                .SetDistinct(true)
                                                                .SetActionTag(AndroidLoadLocalArtistAlbumsRequest.ActionTagDefault);
        }

        protected override void OnRequestResultImpl(object sender, ResultEventArgs args)
        {
            try
            {
                if (args.Request.Status != RequestStatus.Ok)
                {
                    _callbacks.ShowMessage(Resource.String.error_cant_load_data);
                    AmwLog.Error(LogTag, "Error loading local album list");
                    return;
                }
            }
            finally
            {
                RequestManager.Instance.TryDisposeRequest(args.Request.RequestId);
            }

            if (_callbacks.UserVisibleHint)
            {
                if (_adapter.Count < 1)
                {
                    var data = ((LoadArtistAlbumsRequestResult)args.Result).Data;
                    _adapter.SetItems(data);
                }
            }
        }

        protected override void OnRequestUpdateImpl(object sender, UpdateEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok
                  && args.Request.Status != RequestStatus.InProgress) return;

            switch (args.UpdateData.UpdateCode)
            {
                case UpdateData.UpdateCodeCachedResultRetrieved:
                    var cachedResult = ((CachedUpdateData)args.UpdateData).CachedResult;
                    var cachedData = ((LoadArtistAlbumsRequestResult)cachedResult).Data;

                    if (_callbacks.UserVisibleHint)
                    {
                        _adapter.SetItems(cachedData);
                        _callbacks.OnContentDataLoaded(cachedData.Count > 0);
                    }

                    break;

                case UpdateData.UpdateCodeIntermediateResultObtained:
                    var wrappedResult = (IntermediateResultUpdateData)args.UpdateData;
                    var result = (LoadArtistAlbumsRequestResult)wrappedResult.RequestResult;

                    _adapter.AddAlbumArts(result.AlbumArts);

                    if (_callbacks.UserVisibleHint)
                    {
                        _adapter.SetItems(result.Data);
                        _callbacks.OnContentDataLoaded(result.Data.Count > 0);
                    }

                    break;

                default:
                    AmwLog.Warn(LogTag, "unexpected request update code received: " + args.UpdateData.UpdateCode);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed == false) return;

            if (disposing)
            {
                Adapter.Dispose();
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}