using System;
using System.Collections.Generic;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using PagerSlidingTabStrip;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace AirMedia.Platform.UI.Library
{
    public class LibraryTabsAdapter : FragmentPagerAdapter, ITabProvider
    {
        public event EventHandler<TabUpdateEventArgs> TabUpdated;
        public event EventHandler<TabUpdateEventArgs> TabUpdateRequired;

        private readonly List<TabItem> _items;
        private TextTabProvider _textTabProvider;

        public override int Count
        {
            get { return _items.Count; }
        }

        public LibraryTabsAdapter(FragmentManager fm) : base(fm)
        {
            _items = new List<TabItem>();
            _textTabProvider = new TextTabProvider(App.Instance, this);
        }

        public void SetItems(IEnumerable<TabItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override Fragment GetItem(int position)
        {
            var type = _items[position].TabFragmentType;
            var fragment = Fragment.Instantiate(App.Instance, Java.Lang.Class.FromType(type).Name);

            return fragment;
        }

        public string GetTitle(int position)
        {
            return _items[position].Title.ToUpper();
        }

        public void RequestTabUpdate(int position, string hint = null)
        {
            OnTabUpdateRequired(position, hint);
        }

        public View GetTab(PagerSlidingTabStrip.PagerSlidingTabStrip owner, ViewGroup root, int position, View recycled = null)
        {
            if (recycled != null)
                return recycled;

            var view = LayoutInflater.From(owner.Context).Inflate(Resource.Layout.View_Tab, root, false);

            return view;
        }

        public void UpdateTab(View view, PagerSlidingTabStrip.PagerSlidingTabStrip owner, int position, string hint = null)
        {
            var textView = view.FindViewById<TextView>(Android.Resource.Id.Title);
            var text = GetPageTitle(position);
            textView.Text = owner.TabTextAllCaps ? text.ToUpper() : text;

            OnTabUpdated(position);
        }

        public void UpdateTabStyle(View view, PagerSlidingTabStrip.PagerSlidingTabStrip owner, int position)
        {
            var textView = view.FindViewById<TextView>(Android.Resource.Id.Title);
            _textTabProvider.UpdateTabStyle(textView, owner, position);
        }

        public new string GetPageTitle(int position)
        {
            return _items[position].Title;
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            return new Java.Lang.String(GetPageTitle(position));
        }

        protected virtual void OnTabUpdated(int position)
        {
            if (TabUpdated != null)
            {
                TabUpdated(this, new TabUpdateEventArgs(position));
            }
        }

        protected virtual void OnTabUpdateRequired(int position, string hint = null)
        {
            if (TabUpdateRequired != null)
            {
                TabUpdateRequired(this, new TabUpdateEventArgs(position, hint));
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _textTabProvider = null;
            }
        }
    }
}