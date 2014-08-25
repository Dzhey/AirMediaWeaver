using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumGridItemsAdapter : BaseAdapter<AlbumGridItem>
    {
        public class ViewHolder : Java.Lang.Object
        {
            public AlbumGridItem Item { get; set; }
            public TextView TitleView { get; set; }
            public ImageView AlbumImage { get; set; }
            public ViewGroup ItemMenuPanel { get; set; }
            public View ClickableView { get; set; }
        }

        public interface ICallbacks
        {
            event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;
            Bitmap GetAlbumArt(long albumId);
        }

        public static readonly string LogTag = typeof (AlbumGridItemsAdapter).Name;

        public event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;
        public event EventHandler<AlbumItemClickEventArgs> ItemClicked;

        private readonly List<AlbumGridItem> _items;
        private ICallbacks _callbacks;
        private AbsListView _listView;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override AlbumGridItem this[int position]
        {
            get { return _items[position]; }
        }

        public AlbumGridItemsAdapter(AbsListView listView, ICallbacks callbacks)
        {
            _items = new List<AlbumGridItem>();
            _callbacks = callbacks;
            _listView = listView;
            _callbacks.AlbumArtLoaded += OnAlbumArtLoaded;
        }

        public void SetItems(IEnumerable<AlbumGridItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public AlbumGridItem GetItemForView(View itemView)
        {
            var holder = itemView.Tag as ViewHolder;

            if (holder == null)
                return null;

            return holder.Item;
        }

        public void OnAlbumArtLoaded(object sender, AlbumArtLoadedEventArgs args)
        {
            if (args.AlbumArt == null) return;

            var holder = FindViewHolderForAlbumId(args.AlbumId);
            if (holder != null)
            {
                holder.AlbumImage.SetImageBitmap(args.AlbumArt);
                var fadeInAnimation = AnimationUtils.LoadAnimation(App.Instance, Resource.Animation.fade_in);
                fadeInAnimation.FillAfter = true;
                holder.AlbumImage.StartAnimation(fadeInAnimation);
            }
        }

        public void UpdateVisibleAlbumArts()
        {
            for (int i = _listView.FirstVisiblePosition, pos = 0; i <= _listView.LastVisiblePosition; i++, pos++)
            {
                var holder = _listView.GetChildAt(pos).Tag as ViewHolder;

                if (holder == null)
                    continue;

                var albumArt = _callbacks.GetAlbumArt(holder.Item.AlbumId);
                holder.AlbumImage.SetImageBitmap(albumArt);
            }
        }

        private static int _viewCount = 0;
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            ViewHolder holder;

            if (convertView == null)
            {
                holder = new ViewHolder();

                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_AlbumGridItem, parent, false);

                holder.AlbumImage = convertView.FindViewById<ImageView>(Resource.Id.image);
                holder.TitleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);

                holder.ClickableView = convertView.FindViewById(Resource.Id.clickableView);
                holder.ClickableView.Click += OnItemClicked;
                holder.ClickableView.Tag = holder;

                holder.ItemMenuPanel = convertView.FindViewById<ViewGroup>(Resource.Id.itemMenuPanel);
                holder.ItemMenuPanel.Click += OnItemMenuClicked;
                holder.ItemMenuPanel.Tag = holder;

                convertView.Tag = holder;
                _viewCount++;
                AmwLog.Info(LogTag, "album item created: " + _viewCount);
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            holder.Item = item;
            if (string.IsNullOrEmpty(item.AlbumName) == false)
            {
                holder.TitleView.Text = item.AlbumName;
            }
            else
            {
                holder.TitleView.SetText(Resource.String.title_unknown_artist);
            }

            Bitmap albumArt = _callbacks.GetAlbumArt(item.AlbumId);
            if (albumArt == null)
            {
                holder.AlbumImage.SetImageResource(Resource.Drawable.album_cover_placeholder);
            }
            else
            {
                holder.AlbumImage.SetImageBitmap(albumArt);
            }

            return convertView;
        }

        private void OnItemClicked(object sender, EventArgs args)
        {
            if (ItemClicked == null) return;

            var senderView = (View) sender;
            var holder = (ViewHolder) senderView.Tag;

            ItemClicked(this, new AlbumItemClickEventArgs(senderView, holder.Item));
        }

        private void OnItemMenuClicked(object sender, EventArgs args)
        {
            if (ItemMenuClicked == null) return;

            var senderView = (View)sender;
            var holder = (ViewHolder)senderView.Tag;

            ItemMenuClicked(this, new AlbumItemClickEventArgs(senderView, holder.Item));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _callbacks.AlbumArtLoaded -= OnAlbumArtLoaded;
                _listView = null;
                _callbacks = null;
                _items.Clear();
            }
        }

        protected ViewHolder FindViewHolderForAlbumId(long albumId)
        {
            for (int i = _listView.FirstVisiblePosition, pos = 0; i <= _listView.LastVisiblePosition; i++, pos++)
            {
                var holder = _listView.GetChildAt(pos).Tag as ViewHolder;

                if (holder == null || holder.Item.AlbumId != albumId)
                    continue;

                return holder;
            }

            return null;
        }
    }
}