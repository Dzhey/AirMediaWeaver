using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistDetailsFragment : AmwFragment
    {
        private PlaylistItemsAdapter _adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new PlaylistItemsAdapter();
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            return base.OnCreateView(inflater, container, savedInstanceState);
        }
    }
}