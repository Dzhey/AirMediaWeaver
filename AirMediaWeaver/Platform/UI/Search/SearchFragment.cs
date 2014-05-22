using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Playlists;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace AirMedia.Platform.UI.Search
{
    public class SearchFragment : MainViewFragment
    {
        public const int FilterAll = 0;
        public const int FilterArtist = 1;
        public const int FilterAlbum = 2;
        public const int FilterGenre = 3;

        private const string SearchRequestTag = "user_tracks_search";

        private ListView _listView;
        private EditText _searchView;
        private Spinner _searchCriteriaSpinner;
        private ISpinnerAdapter _navigationAdapter;
        private PlaylistTracksAdapter _tracksAdapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _tracksAdapter = new PlaylistTracksAdapter();

            var items = Resources.GetStringArray(Resource.Array.filter_items);
            var adapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleSpinnerItem, items);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleDropDownItem1Line);

            _navigationAdapter = adapter;
        }

        public override View OnCreateView(LayoutInflater inflater,
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_Search, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _tracksAdapter;

            _searchView = view.FindViewById<EditText>(Resource.Id.editText);
            
            _searchCriteriaSpinner = view.FindViewById<Spinner>(Resource.Id.spinner);
            _searchCriteriaSpinner.Adapter = _navigationAdapter;

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            _searchCriteriaSpinner.ItemSelected += OnFilteringItemSelected;
            _searchView.EditorAction += OnSearchEditorAction;

            RegisterRequestResultHandler(typeof(PerformTracksSearchRequest), OnSearchRequestFinished);
        }

        public override void OnPause()
        {
            _searchCriteriaSpinner.ItemSelected -= OnFilteringItemSelected;
            _searchView.EditorAction -= OnSearchEditorAction;

            RemoveRequestResultHandler(typeof(PerformTracksSearchRequest));

            base.OnPause();
        }

        private void SubmitSearch(string searchString)
        {
            searchString = searchString.Trim();
            _searchView.Text = searchString;

            if (string.IsNullOrWhiteSpace(searchString))
            {
                ShowMessage(Resource.String.hint_empty_search_input);
                return;
            }

            SetInProgress(true);
            SubmitParallelRequest(new PerformTracksSearchRequest(TrackSearchCriteria.Artist, searchString)
                {
                    ActionTag = SearchRequestTag
                });
        }

        private void OnFilteringItemSelected(object sender, AdapterView.ItemSelectedEventArgs args)
        {
            switch (args.Position)
            {
                case FilterAll:
                    break;

                case FilterArtist:
                    break;

                case FilterAlbum:
                    break;

                case FilterGenre:
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format("undefined filter item selected: {0}", args.Position));
                    break;
            }
        }

        private void OnSearchEditorAction(object sender, TextView.EditorActionEventArgs args)
        {
            if (args.ActionId == ImeAction.Done)
            {
                SubmitSearch(_searchView.Text);
            }
        }

        private void OnSearchRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "error searching for tracks");
                ShowMessage(Resource.String.error_cant_load_search_results);
                SetInProgress(false);

                return;
            }

            var items = ((LoadRequestResult<List<ITrackMetadata>>)args.Result).Data;
            _tracksAdapter.SetItems(items);

            SetInProgress(false);
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_search);
        }

        public override void OnGenericPlaybackRequested()
        {
            // TODO: generic playback
        }

        public override bool HasDisplayedContent()
        {
            return true;
        }
    }
}