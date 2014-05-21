using System;
using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Platform.UI.Base;
using Android.Views;

namespace AirMedia.Platform.UI.Publications
{
    public class DownloadTrackListAdapter : AbsTrackListAdapter<IRemoteTrackMetadata>
    {
        public class ItemDownloadClickEventArgs : EventArgs
        {
            public IRemoteTrackMetadata TrackMetadata { get; set; }
        }

        protected new class ViewHolder : AbsTrackListAdapter<IRemoteTrackMetadata>.ViewHolder
        {
            public View ButtonDownload { get; set; }
        }

        public event EventHandler<ItemDownloadClickEventArgs> DownloadClicked;

        public bool DisplayDownloadButton
        {
            get
            {
                return _displayDownloadButton;
            }
            set
            {
                if (value == _displayDownloadButton) return;

                _displayDownloadButton = value;
                NotifyDataSetChanged();
            }
        }

        private bool _displayDownloadButton;
        private readonly ISet<string> _downloadedTrackGuids;

        public DownloadTrackListAdapter()
        {
            _downloadedTrackGuids = new HashSet<string>();
        }

        public void AddDownloadTrackGuid(string trackGuid)
        {
            if (_downloadedTrackGuids.Contains(trackGuid)) return;

            _downloadedTrackGuids.Add(trackGuid);
            NotifyDataSetChanged();
        }

        public void SetDownloadTrackGuids(IEnumerable<string> downloadedTrackGuids)
        {
            _downloadedTrackGuids.Clear();
            _downloadedTrackGuids.UnionWith(downloadedTrackGuids);
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        protected override void BindView(View view, AbsTrackListAdapter<IRemoteTrackMetadata>.ViewHolder holder,
            IRemoteTrackMetadata item)
        {
            base.BindView(view, holder, item);

            var holderTyped = (ViewHolder) holder;

            bool isDownloaded = _downloadedTrackGuids.Contains(holder.Item.TrackGuid);
            if (DisplayDownloadButton && isDownloaded == false)
            {
                holderTyped.ButtonDownload.Visibility = ViewStates.Visible;
            }
            else
            {
                holderTyped.ButtonDownload.Visibility = ViewStates.Gone;
            }

            holder.TitleView.Text = item.TrackTitle;
            holder.ArtistView.Text = item.Artist;

            holderTyped.ButtonDownload.Tag = holder;
        }

        protected override View InflateView(LayoutInflater inflater, ViewGroup parent)
        {
            return inflater.Inflate(Resource.Layout.View_DownloadTrackItem, parent, false);
        }

        protected override AbsTrackListAdapter<IRemoteTrackMetadata>.ViewHolder CreateViewHolder(View view)
        {
            var holder = new ViewHolder();

            holder.ButtonDownload = view.FindViewById(Resource.Id.ButtonDownload);
            holder.ButtonDownload.Click += OnDownloadButtonClicked;

            return holder;
        }

        private void OnDownloadButtonClicked(object sender, EventArgs args)
        {
            if (DownloadClicked == null) return;

            var holder = (ViewHolder)((View)sender).Tag;

            DownloadClicked(this, new ItemDownloadClickEventArgs { TrackMetadata = holder.Item });
        }
    }
}