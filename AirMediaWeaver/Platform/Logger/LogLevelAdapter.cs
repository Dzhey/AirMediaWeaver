using System.Collections.Generic;
using AirMedia.Core.Log;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.Logger
{
    public struct LogLevelItem
    {
        public LogLevel Level { get; set; }
    }

    public class LogLevelAdapter : BaseAdapter<LogLevelItem>
    {
        private readonly List<LogLevelItem> _items;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override LogLevelItem this[int position]
        {
            get { return _items[position]; }
        }

        public LogLevelAdapter()
        {
            _items = new List<LogLevelItem>();
        }

        public int FindItemPosition(LogLevel level)
        {
            return _items.FindIndex(item => item.Level == level);
        }

        public void SetItems(IEnumerable<LogLevelItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context).Inflate(
                    global::Android.Resource.Layout.SimpleSpinnerItem, parent, false);
            }

            ((TextView) convertView).Text = item.Level.ToString();

            return convertView;
        }
    }
}