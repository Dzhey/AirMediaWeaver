using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.Logger;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library.AlbumList.Adapter;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public class AlbumsCategorizedGridWorker : BaseContextualRequestWorker, IAlbumListContentWorker
    {
        public static readonly string LogTag = typeof(AlbumsCategorizedGridWorker).Name;

        public bool IsAlbumArtsLoaderEnabled
        {
            get
            {
                if (_coverProvider == null) return false;

                return _coverProvider.IsAlbumArtsLoaderEnabled;
            }
            set
            {
                if (_coverProvider == null) return;

                _coverProvider.IsAlbumArtsLoaderEnabled = value;
            }
        }
        
        public AbsListView InflateContainerView(LayoutInflater inflater, ViewGroup root, bool attachToRoot)
        {
            var container = (ViewGroup)inflater.Inflate(Resource.Layout.View_AlbumsGridList, root, attachToRoot);

            return container.FindViewById<AbsListView>(Resource.Id.albumsGrid);
        }

        public IAlbumListAdapter Adapter { get { return _adapter; } }

        private readonly IConcreteAlbumListAdapter<AlbumCategorizedGridEntry> _adapter;
        private bool _isDisposed;
        private readonly IAlbumListContentWorkerCallbacks _callbacks;
        private readonly IAlbumsCoverProvider _coverProvider;

        public AlbumsCategorizedGridWorker(IAlbumListContentWorkerCallbacks callbacks) : base(callbacks)
        {
            _coverProvider = new AlbumsCoverProvider();
            _adapter = new AlbumGridListAdapter(_coverProvider);
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

                    _coverProvider.AddAlbumArts(result.AlbumArts);

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
                if (_coverProvider != null)
                {
                    _coverProvider.Dispose();
                }
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}