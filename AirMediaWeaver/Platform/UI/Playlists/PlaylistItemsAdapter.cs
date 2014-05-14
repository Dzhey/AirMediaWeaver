using System.Collections.Generic;
using AirMedia.Platform.Data;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistItemsAdapter : BaseAdapter<TrackMetadata>
    {
        private class ViewHolder : Java.Lang.Object
        {
            public TrackMetadata Metadata { get; set; }
            public TextView TitleView { get; set; }
            public TextView ArtistView { get; set; }
        }

        private readonly List<TrackMetadata> _items;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override TrackMetadata this[int position]
        {
            get { return _items[position]; }
        }

        public PlaylistItemsAdapter()
        {
            _items = new List<TrackMetadata>();
        }

        public void SetItems(IEnumerable<TrackMetadata> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return _items[position].TrackId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;

            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_TrackItem, parent, false);
                holder = new ViewHolder();
                holder.TitleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
                holder.ArtistView = convertView.FindViewById<TextView>(Resource.Id.artist);
                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            var item = this[position];

            holder.Metadata = item;
            holder.TitleView.Text = item.TrackTitle;
            holder.ArtistView.Text = item.ArtistName;

            return convertView;
        }
    }
}