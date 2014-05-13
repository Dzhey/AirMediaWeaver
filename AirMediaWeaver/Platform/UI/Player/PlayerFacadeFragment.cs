using System;
using AirMedia.Platform.Data;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Player
{
    public class PlayerFacadeFragment : AmwFragment, IMediaPlayerCallbacks
    {
        private MediaServiceConnection _mediaServiceConnection;
        private ViewGroup _trackInfoPanel;
        private TextView _trackTitleView;
        private TextView _trackArtistView;
        private SeekBar _seekBar;
        private ToggleButton _buttonRewind;
        private ToggleButton _buttonTogglePlayback;
        private ToggleButton _buttonFastForward;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _mediaServiceConnection = new MediaServiceConnection(this);

            var intent = new Intent(Activity, typeof(MediaPlayerService));
            Activity.BindService(intent, _mediaServiceConnection, Bind.AutoCreate | Bind.Important);
        }

        public override void OnDestroy()
        {
            if (_mediaServiceConnection.IsBound)
            {
                _mediaServiceConnection.Release();
                Activity.UnbindService(_mediaServiceConnection);
            }

            base.OnDestroy();
        }

        private void UpdatePanelIndicators(bool isStopped)
        {
            if (isStopped)
            {
                _trackInfoPanel.Visibility = ViewStates.Gone;
                _seekBar.Visibility = ViewStates.Gone;
            }
            else
            {
                _trackInfoPanel.Visibility = ViewStates.Visible;
                _seekBar.Visibility = ViewStates.Visible;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_PlayerFacade, container, false);

            _seekBar = view.FindViewById<SeekBar>(Resource.Id.seekBar);

            _trackInfoPanel = view.FindViewById<ViewGroup>(Resource.Id.trackInfoPanel);
            _trackTitleView = _trackInfoPanel.FindViewById<TextView>(Resource.Id.trackTitle);
            _trackArtistView = _trackInfoPanel.FindViewById<TextView>(Resource.Id.trackArtist);

            _buttonRewind = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerRewind);
            _buttonTogglePlayback = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerPlay);
            _buttonFastForward = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerFF);

            SetupPlayerButton(_buttonRewind, Resource.Drawable.button_player_rew_selector);
            SetupPlayerButton(_buttonTogglePlayback, Resource.Drawable.button_player_play_selector);
            SetupPlayerButton(_buttonFastForward, Resource.Drawable.button_player_ff_selector);

            bool isStopped = true;
            if (_mediaServiceConnection.IsBound)
            {
                isStopped = !_mediaServiceConnection.Binder.Service.IsPlaying();
            }
            UpdatePanelIndicators(isStopped);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            _buttonTogglePlayback.Click += OnPlayToggleClicked;
            _buttonRewind.Click += OnRewindClicked;
            _buttonFastForward.Click += OnFastForwardClicked;
        }

        public override void OnPause()
        {
            _buttonTogglePlayback.Click -= OnPlayToggleClicked;
            _buttonRewind.Click -= OnRewindClicked;
            _buttonFastForward.Click -= OnFastForwardClicked;

            base.OnPause();
        }

        private void OnPlayToggleClicked(object sender, EventArgs args)
        {
            _buttonTogglePlayback.Checked = !_buttonTogglePlayback.Checked;
        }

        private void OnRewindClicked(object sender, EventArgs args)
        {
        }

        private void OnFastForwardClicked(object sender, EventArgs args)
        {
        }

        private void SetupPlayerButton(ToggleButton button, int drawableResourceId)
        {
            var span = new ImageSpan(Activity, drawableResourceId);
            var content = new SpannableString("X");
            content.SetSpan(span, 0, 1, SpanTypes.ExclusiveExclusive);
            button.TextFormatted = content;
            button.TextOnFormatted = content;
            button.TextOffFormatted = content;
        }

        public void OnPlaybackStarted()
        {
            _buttonTogglePlayback.Checked = true;
            UpdatePanelIndicators(false);
        }

        public void OnPlaybackStopped()
        {
            _buttonTogglePlayback.Checked = false;
            UpdatePanelIndicators(true);
        }

        public void OnTrackMetadataResolved(TrackMetadata metadata)
        {
            _trackTitleView.Text = metadata.TrackTitle;
            _trackArtistView.Text = metadata.ArtistName;
        }
    }
}