using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Library.AlbumList.Controller;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace AirMedia.Platform.UI.Library.AlbumList.Adapter
{
    public class AlbumGridListAdapter : BaseAdapter<AlbumCategorizedGridEntry>, IConcreteAlbumListAdapter<AlbumCategorizedGridEntry>
    {
        private class ViewHolder : Java.Lang.Object
        {
            public AlbumCategorizedGridEntry Item { get; set; }
            public AlbumGridItemsAdapter GridAdapter { get; set; }
            public TextView TitleView { get; set; }
            public GridView ItemsGrid { get; set; }
        }

        public static readonly string LogTag = typeof(AlbumGridListAdapter).Name;

        public event EventHandler<AlbumItemClickEventArgs> ItemClicked;
        public event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;

        public IAlbumsCoverProvider AlbumsCoverProvider
        {
            get { return _albumsCoverProvider; }
            set
            {
                if (_albumsCoverProvider != null)
                {
                    _albumsCoverProvider.AlbumArtsLoaderEnabled -= OnAlbumArtsLoaderEnabled;
                }

                _albumsCoverProvider = value;
                if (value != null)
                {
                    _albumsCoverProvider.AlbumArtsLoaderEnabled += OnAlbumArtsLoaderEnabled;
                }
            }
        }

        public IAlbumListAdapterCallbacks Callbacks
        {
            get
            {
                if (_callbacks == null)
                {
                    throw new IllegalStateException("album adapter callbacks is not defined");
                }

                return _callbacks;
            }

            set { _callbacks = value; }
        }

        private readonly List<AlbumCategorizedGridEntry> _items;
        private IAlbumListAdapterCallbacks _callbacks;
        private bool _isDisposed;
        private IAlbumsCoverProvider _albumsCoverProvider;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override AlbumCategorizedGridEntry this[int position]
        {
            get { return _items[position]; }
        }

        public AlbumGridListAdapter() : this(null)
        {
        }

        public AlbumGridListAdapter(IAlbumsCoverProvider coverProvider)
        {
            _items = new List<AlbumCategorizedGridEntry>();
            AlbumsCoverProvider = coverProvider;
        }

        public void SetItems(IEnumerable<AlbumCategorizedGridEntry> items)
        {
            if (_isDisposed) return;

            _items.Clear();
            _items.AddRange(items);

            App.MainHandler.Post(NotifyDataSetChanged);
        }

        public void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts)
        {
            if (_albumsCoverProvider != null)
                _albumsCoverProvider.AddAlbumArts(albumArts);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        private int _viewCount = 0;
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            ViewHolder holder;

            if (convertView == null)
            {
                holder = new ViewHolder();
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_AlbumListBlockEntry, parent, false);

                holder.TitleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
                holder.ItemsGrid = convertView.FindViewById<GridView>(Resource.Id.albumsGrid);

                holder.GridAdapter = new AlbumGridItemsAdapter(holder.ItemsGrid, _albumsCoverProvider);
                holder.GridAdapter.ItemMenuClicked += OnGridItemMenuClicked;
                holder.GridAdapter.ItemClicked += OnGridItemClicked;

                holder.ItemsGrid.Adapter = holder.GridAdapter;

                convertView.Tag = holder;
                _viewCount++;
                AmwLog.Info(LogTag, "grid created: " + _viewCount);
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }

            holder.Item = item;

            string artistName = string.IsNullOrEmpty(item.ArtistName)
                                    ? parent.Context.GetString(Resource.String.title_unknown_artist)
                                    : item.ArtistName;

            holder.TitleView.Text = artistName.ToUpper();

            holder.GridAdapter.SetItems(item.Albums);

            return convertView;
        }

        private void UpdateVisibleAlbumArts()
        {
            var listView = Callbacks.GetListView();

            if (listView == null)
                return;

            for (int i = listView.FirstVisiblePosition, pos = 0; i <= listView.LastVisiblePosition; i++, pos++)
            {
                var holder = listView.GetChildAt(pos).Tag as ViewHolder;

                if (holder == null)
                    continue;

                holder.GridAdapter.UpdateVisibleAlbumArts();
            }
        }

        private void OnGridItemClicked(object sender, AlbumItemClickEventArgs args)
        {
            if (ItemClicked == null) return;

            ItemClicked(this, args);
        }

        private void OnGridItemMenuClicked(object sender, AlbumItemClickEventArgs args)
        {
            if (ItemMenuClicked == null) return;

            ItemMenuClicked(this, args);
        }

        private void OnAlbumArtsLoaderEnabled(object sender, AlbumArtsLoaderEnabledEventArgs args)
        {
            if (args.IsAlbumArtsEnabled)
                UpdateVisibleAlbumArts();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            base.Dispose(disposing);

            if (disposing)
            {
                if (_albumsCoverProvider != null)
                {
                    _albumsCoverProvider.AlbumArtsLoaderEnabled -= OnAlbumArtsLoaderEnabled;
                }
                _callbacks = null;
            }

            _isDisposed = true;
        }
    }
}