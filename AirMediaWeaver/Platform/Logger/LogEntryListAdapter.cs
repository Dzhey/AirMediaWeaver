using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.Logger
{
    public class LogEntryListAdapter : BaseAdapter<LogEntryRecord>
    {
        private class ViewHolder : Java.Lang.Object
        {
            public LogEntryRecord Item { get; set; }
            public TextView IndicatorLeft { get; set; }
            public TextView IndicatorRight { get; set; }
            public ImageButton ButtonDetails { get; set; }
        }

        private readonly List<LogEntryRecord> _items;
        private readonly ISet<int> _expandedItems;
        private AbsListView _listView;
        private bool _isAllItemsExpanded;

        public event EventHandler<LogEntryRecord> ItemDetailsClicked;

        public bool IsAllItemsExpanded
        {
            get
            {
                return _isAllItemsExpanded;
            }
            set
            {
                _isAllItemsExpanded = value; 

                if (_isAllItemsExpanded)
                {
                    _expandedItems.Clear();
                }

                NotifyDataSetChanged();
            }
        }

        public override long GetItemId(int position)
        {
            return this[position].Id;
        }

        public override int Count
        {
            get { return _items.Count; }
        }

        public override LogEntryRecord this[int position]
        {
            get { return _items[position]; }
        }

        public LogEntryListAdapter(AbsListView listView)
        {
            _items = new List<LogEntryRecord>();
            _expandedItems = new HashSet<int>();
            _listView = listView;
        }

        public bool IsItemExpanded(int itemId)
        {
            return IsAllItemsExpanded || _expandedItems.Contains(itemId);
        }

        public void ToggleItem(int itemId)
        {
            if (_expandedItems.Contains(itemId))
            {
                _expandedItems.Remove(itemId);
            }
            else
            {
                _expandedItems.Add(itemId);
            }

            UpdateView(itemId);
        }

        public void ExpandItem(int itemId)
        {
            _expandedItems.Add(itemId);
            UpdateView(itemId);
        }

        public void CollapseItem(int itemId)
        {
            if (_expandedItems.Remove(itemId))
            {
                UpdateView(itemId);
            }
        }

        public void SetItems(IEnumerable<LogEntryRecord> records)
        {
            _items.Clear();
            _items.AddRange(records);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;
            if (convertView == null)
            {
                holder = new ViewHolder();
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_LogEntry, parent, false);
                holder.IndicatorLeft = convertView.FindViewById<TextView>(Resource.Id.indicatorLeft);
                holder.IndicatorRight = convertView.FindViewById<TextView>(Resource.Id.indicatorRight);
                holder.ButtonDetails = convertView.FindViewById<ImageButton>(Resource.Id.buttonDetails);
                holder.ButtonDetails.Click += OnDetailsButtonClicked;

                holder.ButtonDetails.Tag = holder;

                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            holder.Item = this[position];

            UpdateView(holder);

            return convertView;
        }

        protected override void Dispose(bool disposing)
        {
            _listView = null;

            base.Dispose(disposing);
        }

        private void OnDetailsButtonClicked(object sender, EventArgs args)
        {
            if (ItemDetailsClicked == null) return;

            var holder = (ViewHolder) ((View) sender).Tag;

            ItemDetailsClicked(this, holder.Item);
        }

        private void UpdateView(int itemId)
        {
            var holder = FindViewHolder(itemId);

            if (holder != null)
            {
                UpdateView(holder);
            }
        }

        private void UpdateView(ViewHolder holder)
        {
            var item = holder.Item;
            var color = LogUtils.GetLogColor(item.Level);

            holder.IndicatorLeft.Text = item.Tag;
            holder.IndicatorLeft.SetTextColor(color);
            holder.IndicatorRight.SetTextColor(color);

            if (IsItemExpanded(item.Id))
            {
                string text = string.Format("{0:dd/MM/yy HH:mm:ss.fff}\n{1}",
                    item.Date,
                    item.Message);
                holder.IndicatorRight.Text = text;
                holder.IndicatorRight.SetMaxLines(int.MaxValue);
                holder.IndicatorLeft.SetMaxLines(int.MaxValue);
            }
            else
            {
                holder.IndicatorRight.Text = item.Message ?? "<no message>";
                holder.IndicatorRight.SetMaxLines(1);
                holder.IndicatorLeft.SetMaxLines(1);
            }

            if (string.IsNullOrEmpty(item.Details))
            {
                holder.ButtonDetails.Visibility = ViewStates.Gone;
            }
            else
            {
                holder.ButtonDetails.Visibility = ViewStates.Visible;
            }

            holder.IndicatorRight.Invalidate();
        }

        private ViewHolder FindViewHolder(int itemId)
        {
            int last = _listView.LastVisiblePosition - _listView.FirstVisiblePosition;
            for (int i = 0; i <= last; i++)
            {
                var view = _listView.GetChildAt(i);

                if (view == null) continue;

                var holder = view.Tag as ViewHolder;

                if (holder != null && holder.Item.Id == itemId)
                {
                    return holder;
                }
            }

            return null;
        }
    }
}