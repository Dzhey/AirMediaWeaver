using System.Collections.Generic;
using AirMedia.Core.Data;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistListAdapter : BaseAdapter<PlaylistModel>
    {
        private readonly List<PlaylistModel> _items;
        private readonly ISet<long> _checkedItems;

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
            _checkedItems = new HashSet<long>();
        }

        public void SetItems(IEnumerable<PlaylistModel> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public void ToggleItemCheck(long itemId)
        {
            bool isChecked = IsItemChecked(itemId);
            SetItemChecked(itemId, !isChecked);
        }

        public void SetItemChecked(long itemId, bool isChecked)
        {
            if (isChecked)
            {
                _checkedItems.Add(itemId);
            }
            else
            {
                _checkedItems.Remove(itemId);
            }
            NotifyDataSetChanged();
        }

        public bool IsItemChecked(long itemId)
        {
            return _checkedItems.Contains(itemId);
        }

        public void ResetCheckedItems()
        {
            _checkedItems.Clear();
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

            if (IsItemChecked(item.Id))
            {
                convertView.SetBackgroundResource(Resource.Color.holo_blue_light_translucent);
            }
            else
            {
                convertView.SetBackgroundResource(Android.Resource.Color.Transparent);
            }

            var titleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
            titleView.Text = item.Name;

            return convertView;
        }
    }
}