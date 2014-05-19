using System.Linq;
using AirMedia.Platform.Data;
using AirMedia.Platform.UI.Base;
using Android.Views;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistTracksAdapter : AbsTrackListAdapter<TrackMetadata>
    {
        public override long GetItemId(int position)
        {
            return this[position].TrackId;
        }

        public long[] GetItemIds()
        {
            return Items.Select(input => input.TrackId).ToArray();
        }

        protected override void BindView(View view, ViewHolder holder, TrackMetadata item)
        {
            holder.TitleView.Text = item.TrackTitle;
            holder.ArtistView.Text = item.Artist;
        }
    }
}