using System.Collections.Generic;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.MainView
{
    public class DrawerListAdapter : BaseAdapter<DrawerNavigationItem>
    {
        private readonly List<DrawerNavigationItem> _items;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override DrawerNavigationItem this[int position]
        {
            get { return _items[position]; }
        }

        public DrawerListAdapter()
        {
            _items = new List<DrawerNavigationItem>();
        }

        public void SetItems(IEnumerable<DrawerNavigationItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return this[position].StringResourceId;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_DrawerNavigationItem, parent, false);
            }

            var titleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
            titleView.SetText(item.StringResourceId);

            return convertView;
        }
    }
}