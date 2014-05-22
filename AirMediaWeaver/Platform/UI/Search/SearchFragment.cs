using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Playlists;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.UI.Search
{
    public class SearchFragment : MainViewFragment
    {
        public const int FilterAll = (int) TrackSearchCriteria.All;
        public const int FilterTitle = (int) TrackSearchCriteria.Title;
        public const int FilterArtist = (int)TrackSearchCriteria.Artist;
        public const int FilterAlbum = (int)TrackSearchCriteria.Album;
        public const int FilterGenre = (int)TrackSearchCriteria.Genre;

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

            var progressPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, Consts.DefaultProgressDelayMillis, 
                Resource.String.hint_no_started_search);
            SetInProgress(false);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            _searchCriteriaSpinner.ItemSelected += OnFilteringItemSelected;
            _searchView.EditorAction += OnSearchEditorAction;
            _listView.ItemClick += OnTrackItemClicked;

            RegisterRequestResultHandler(typeof(PerformTracksSearchRequest), OnSearchRequestFinished);
        }

        public override void OnPause()
        {
            _searchCriteriaSpinner.ItemSelected -= OnFilteringItemSelected;
            _searchView.EditorAction -= OnSearchEditorAction;
            _listView.ItemClick -= OnTrackItemClicked;

            RemoveRequestResultHandler(typeof(PerformTracksSearchRequest));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_search);
        }

        public override void OnGenericPlaybackRequested()
        {
            if (_tracksAdapter.Count < 1)
            {
                ShowMessage(Resource.String.error_cant_play_search_results_list_empty);
                return;
            }

            PlayerControl.Play(_tracksAdapter.Items, 0);
        }

        public override bool HasDisplayedContent()
        {
            return _listView.Count > 0;
        }

        private void OnTrackItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            PlayerControl.Play(_tracksAdapter.Items, args.Position);
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
            SetEmptyContentMessage(GetString(Resource.String.hint_empty_search_results));
            var criteria = (TrackSearchCriteria)_searchCriteriaSpinner.SelectedItemPosition;
            SubmitParallelRequest(new PerformTracksSearchRequest(criteria, searchString)
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

                case FilterTitle:
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
                var imm = (InputMethodManager)Activity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromInputMethod(_searchView.WindowToken, 0);
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
    }
}