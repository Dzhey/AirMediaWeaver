using AirMedia.Core;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumListFragment : MainViewFragment, AlbumListGridAdapter.ICallbacks
    {
        private ListView _albumListView;
        private AlbumListGridAdapter _listAdapter;
        private IRequestFactory _loadRequestFactory;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _listAdapter = new AlbumListGridAdapter(this);
            var factory = RequestFactory.Init(typeof (AndroidLoadLocalArtistAlbumsRequest));
            _loadRequestFactory = AndroidRequestFactory.Init(factory, ResultListener)
                                                       .SetParallel(true)
                                                       .SetDistinct(true)
                                                       .SetActionTag(AndroidLoadLocalArtistAlbumsRequest.ActionTagDefault);
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
           var view = inflater.Inflate(Resource.Layout.Fragment_AlbumList, container, false);

           _albumListView = view.FindViewById<ListView>(Android.Resource.Id.List);
            var progresPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progresPanel, Consts.DefaultProgressDelayMillis, 
                Resource.String.note_audio_library_empty);

            _albumListView.Adapter = _listAdapter;

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RegisterRequestUpdateHandler(typeof(AndroidLoadLocalArtistAlbumsRequest), OnLoadRequestUpdate);
            ReloadList();
        }

        public override void OnDestroyView()
        {
            RemoveRequestUpdateHandler(typeof(AndroidLoadLocalArtistAlbumsRequest));

            base.OnDestroyView();
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(AndroidLoadLocalArtistAlbumsRequest), OnLoadRequestFinished);
        }

        public override void OnPause()
        {
            RemoveRequestResultHandler(typeof(AndroidLoadLocalArtistAlbumsRequest));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_tab_albums);
        }

        public override void OnGenericPlaybackRequested()
        {
        }

        public override bool HasDisplayedContent()
        {
            return _listAdapter.Count > 0;
        }

        protected void ReloadList()
        {
            SetInProgress(true);
            _loadRequestFactory.Submit();
        }

        private void OnLoadRequestFinished(object sender, ResultEventArgs args)
        {
            SetInProgress(false);
            if (args.Request.Status != RequestStatus.Ok)
            {
                ShowMessage(Resource.String.error_cant_load_data);
                AmwLog.Error(LogTag, "Error loading local album list");
            }

            RequestManager.Instance.TryDisposeRequest(args.Request.RequestId);
        }

        private void OnLoadRequestUpdate(object sender, UpdateEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok
                  && args.Request.Status != RequestStatus.InProgress) return;

            switch (args.UpdateData.UpdateCode)
            {
                case UpdateData.UpdateCodeCachedResultRetrieved:
                    var cachedResult = ((CachedUpdateData)args.UpdateData).CachedResult;
                    var cachedData = ((LoadArtistAlbumsRequestResult)cachedResult).Data;
                    _listAdapter.SetItems(cachedData);
                    
                    if (cachedData.Count > 0)
                        SetInProgress(false);
                    break;

                case UpdateData.UpdateCodeIntermediateResultObtained:
                    var wrappedResult = (IntermediateResultUpdateData) args.UpdateData;
                    var result = (LoadArtistAlbumsRequestResult) wrappedResult.RequestResult;
                    _listAdapter.SetItems(result.Data);
                    _listAdapter.AddAlbumArts(result.AlbumArts);
                    if (result.Data.Count > 0)
                        SetInProgress(false);
                    break;
            }
        }

        public void OnLowMemoryDetected()
        {
            ShowMessage(Resource.String.warning_out_of_memory);
        }
    }
}