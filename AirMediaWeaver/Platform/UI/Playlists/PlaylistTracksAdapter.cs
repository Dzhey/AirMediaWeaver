using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Platform.UI.Base;
using Android.Views;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistTracksAdapter : AbsTrackListAdapter<ITrackMetadata>
    {
        public override long GetItemId(int position)
        {
            return this[position].TrackId;
        }

        public long[] GetItemIds()
        {
            return Items.Select(input => input.TrackId).ToArray();
        }

        protected override void BindView(View view, ViewHolder holder, ITrackMetadata item)
        {
            holder.TitleView.Text = item.TrackTitle;
            holder.ArtistView.Text = item.Artist;
        }
    }
}