using AirMedia.Core.Log;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace AirMedia.Platform.UI.Library
{
    public class AudioLibraryFragment : AmwFragment, LoaderManager.ILoaderCallbacks
    {
        private const int TrackListLoaderId = 1;

        private ListView _listView;
        private TrackListAdapter _adapter;
        private View _progressPanel;
        private View _emptyIndicatorView;

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_AudioLibrary, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _progressPanel = view.FindViewById(Android.Resource.Id.Progress);
            _emptyIndicatorView = view.FindViewById(Android.Resource.Id.Empty);

            if (_adapter != null)
            {
                _listView.Adapter = _adapter;
            }

            LoaderManager.InitLoader(TrackListLoaderId, null, this);
            UpdateProgressIndicators(true);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            _listView.ItemClick += OnTrackItemClicked;
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnTrackItemClicked;

            base.OnPause();
        }

        public Loader OnCreateLoader(int id, Bundle args)
        {
            switch (id)
            {
                case TrackListLoaderId:
                    return new TrackListLoader(Activity);

                default:
                    AmwLog.Error(LogTag, string.Format("undefined loader \"{0}\" requested", id));
                    return null;
            }
        }

        public void OnLoaderReset(Loader loader)
        {
            _listView.Adapter = null;
            _adapter = null;
        }

        public void OnLoadFinished(Loader loader, Object data)
        {
            _adapter = new TrackListAdapter(Activity, (ICursor) data);

            if (_listView != null)
            {
                _listView.Adapter = _adapter;
                _listView.Post(() => _adapter.NotifyDataSetChanged());
            }

            UpdateProgressIndicators(false);
        }

        private void OnTrackItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            long trackId = _adapter.GetTrackId(args.View);

            if (!PlayerControl.Play(trackId))
            {
                ShowMessage(Resource.String.error_unable_to_retrieve_track);
            }
        }

        private void UpdateProgressIndicators(bool isInProgress)
        {
            if (_listView == null || _listView.Count == 0)
            {
                if (isInProgress)
                {
                    _progressPanel.Visibility = ViewStates.Visible;
                    _emptyIndicatorView.Visibility = ViewStates.Gone;
                }
                else
                {
                    _progressPanel.Visibility = ViewStates.Gone;
                    _emptyIndicatorView.Visibility = ViewStates.Visible;
                }
            }
            else
            {
                _progressPanel.Visibility = ViewStates.Gone;
                _emptyIndicatorView.Visibility = ViewStates.Gone;
            }
        }
    }
}