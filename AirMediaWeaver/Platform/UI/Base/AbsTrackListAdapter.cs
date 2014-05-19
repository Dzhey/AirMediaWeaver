using System.Collections.Generic;
using AirMedia.Core.Data;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Base
{
    public abstract class AbsTrackListAdapter<TItem> : 
        BaseAdapter<TItem> where TItem : ITrackSourceDefinition
    {
        protected class ViewHolder : Java.Lang.Object
        {
            public TItem Item { get; set; }
            public TextView TitleView { get; set; }
            public TextView ArtistView { get; set; }
            public CheckBox CheckBox { get; set; }
        }

        public override int Count
        {
            get { return _items.Count; }
        }

        public IReadOnlyCollection<TItem> Items
        {
            get
            {
                return _items.AsReadOnly();
            }
        }

        private readonly List<TItem> _items;

        public override TItem this[int position]
        {
            get { return _items[position]; }
        }

        protected AbsTrackListAdapter()
        {
            _items = new List<TItem>();
        }

        public void SetItems(IEnumerable<TItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public string[] GetItemGuids()
        {
            return _items.ConvertAll(input => input.TrackGuid).ToArray();
        }

        public sealed override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;

            if (convertView == null)
            {
                convertView = InflateView(LayoutInflater.From(parent.Context), parent);
                holder = CreateViewHolder(convertView);
                holder.TitleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
                holder.ArtistView = convertView.FindViewById<TextView>(Resource.Id.artist);
                holder.CheckBox = convertView.FindViewById<CheckBox>(Android.Resource.Id.Checkbox);
                holder.CheckBox.Visibility = ViewStates.Gone;
                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            var item = this[position];

            BindView(convertView, holder, item);

            return convertView;
        }

        protected virtual void BindView(View view, ViewHolder holder, TItem item)
        {
            holder.Item = item;
        }

        protected virtual ViewHolder CreateViewHolder(View view)
        {
            return new ViewHolder();
        }

        protected virtual View InflateView(LayoutInflater inflater, ViewGroup parent)
        {
            return inflater.Inflate(Resource.Layout.View_TrackItem, parent, false);
        }
    }
}