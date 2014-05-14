using System.Collections.Generic;
using AirMedia.Core.Data;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistListAdapter : BaseAdapter<PlaylistModel>
    {
        private readonly List<PlaylistModel> _items;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override PlaylistModel this[int position]
        {
            get { return _items[position]; }
        }

        public override long GetItemId(int position)
        {
            return this[position].Id;
        }

        public PlaylistListAdapter()
        {
            _items = new List<PlaylistModel>();
        }

        public void SetItems(IEnumerable<PlaylistModel> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_PlaylistItem, parent, false);
            }

            var titleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
            titleView.Text = item.Name;

            return convertView;
        }
    }
}