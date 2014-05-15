using System;
using AirMedia.Platform.Data;
using AirMedia.Platform.Player;
using AirMedia.Platform.Player.MediaService;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Player
{
    public class PlayerFacadeFragment : AmwFragment, 
        IMediaPlayerCallbacks, 
        SeekBar.IOnSeekBarChangeListener, 
        MediaServiceConnection.IConnectionListener
    {
        private static readonly string UtfDash = Char.ConvertFromUtf32(8211);
        
        private MediaServiceConnection _mediaServiceConnection;
        private ViewGroup _trackInfoPanel;
        private TextView _trackInfoView;
        private SeekBar _seekBar;
        private ToggleButton _buttonRewind;
        private ToggleButton _buttonTogglePlayback;
        private ToggleButton _buttonFastForward;
        private bool _isTouchingSeekBar;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _mediaServiceConnection = new MediaServiceConnection(this, this);

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
            _trackInfoView = _trackInfoPanel.FindViewById<TextView>(Resource.Id.trackInfo);
            _trackInfoView.Text = "";

            _buttonRewind = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerRewind);
            _buttonTogglePlayback = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerPlay);
            _buttonFastForward = view.FindViewById<ToggleButton>(Resource.Id.buttonPlayerFF);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            _seekBar.SetOnSeekBarChangeListener(this);
            _buttonTogglePlayback.Click += OnPlayToggleClicked;
            _buttonRewind.Click += OnRewindClicked;
            _buttonFastForward.Click += OnFastForwardClicked;
        }

        public override void OnPause()
        {
            _seekBar.SetOnSeekBarChangeListener(null);
            _buttonTogglePlayback.Click -= OnPlayToggleClicked;
            _buttonRewind.Click -= OnRewindClicked;
            _buttonFastForward.Click -= OnFastForwardClicked;

            base.OnPause();
        }

        private void OnPlayToggleClicked(object sender, EventArgs args)
        {
            if (_mediaServiceConnection.Binder.TogglePause() == false)
            {
                _buttonTogglePlayback.Checked = false;
            }
        }

        private void OnRewindClicked(object sender, EventArgs args)
        {
        }

        private void OnFastForwardClicked(object sender, EventArgs args)
        {
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
            DisplayTrackMetadata(metadata);
        }

        public void OnPlaybackProgressUpdate(int current, int duration)
        {
            if (_seekBar == null || _isTouchingSeekBar) return;

            _seekBar.Max = duration;
            _seekBar.Progress = current;
        }

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
            _isTouchingSeekBar = true;
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
            if (_mediaServiceConnection.IsBound)
            {
                _mediaServiceConnection.Binder.SeekTo(seekBar.Progress);
            }
            _isTouchingSeekBar = false;
        }

        public void OnServiceConnected(MediaPlayerBinder binder)
        {
            bool isPlaying = binder.IsPlaying();
            _buttonTogglePlayback.Checked = isPlaying;
            UpdatePanelIndicators(!isPlaying);

            var metadata = binder.GetTrackMetadata();
            if (metadata != null)
            {
                DisplayTrackMetadata((TrackMetadata) metadata);
            }
        }

        public void OnServiceDisconnected()
        {
        }

        private void DisplayTrackMetadata(TrackMetadata metadata)
        {
            _trackInfoView.Text = string.Format("{0} {1} {2}", metadata.ArtistName,
                UtfDash, metadata.TrackTitle);
        }
    }
}