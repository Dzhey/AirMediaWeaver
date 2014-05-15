using Android.Content;
using Android.Database;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library
{
    public class TrackListAdapter : CursorAdapter
    {
        private class ViewHolder : Java.Lang.Object
        {
            public long TrackId { get; set; }
            public TextView TitleView { get; set; }
            public TextView ArtistView { get; set; }
            public CheckBox CheckBox { get; set; }
        }

        private bool _shouldDisplayCheckboxes;
        private readonly ITrackListAdapterCallbacks _callbacks;

        public bool ShouldDisplayCheckboxes
        {
            get { return _shouldDisplayCheckboxes; }
            set
            {
                _shouldDisplayCheckboxes = value;
                NotifyDataSetChanged();
            }
        }

        public override bool HasStableIds
        {
            get
            {
                return true;
            }
        }

        public TrackListAdapter(Context context, ITrackListAdapterCallbacks callbacks, ICursor c) 
            : base(context, c, CursorAdapterFlags.None)
        {
            _callbacks = callbacks;
        }

        public long[] GetDisplayedTrackIds()
        {
            int? idColumn = null;
            int count = Count;
            var result = new long[count];

            for (int i = 0; i < count; i++)
            {
                var cursor = (ICursor)GetItem(i);

                if (idColumn == null)
                {
                    idColumn = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                }

                result[i] = cursor.GetLong((int)idColumn);
            }

            return result;
        }

        public int FindItemPosition(long itemId)
        {
            int? idColumn = null;
            for (int i = 0; i < Count; i++)
            {
                var cursor = (ICursor)GetItem(i);

                if (idColumn == null)
                {
                    idColumn = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                }

                if (cursor.GetLong((int) idColumn) == itemId) return i;
            }

            return -1;
        }

        public long GetTrackId(View view)
        {
            var holder = (ViewHolder) view.Tag;

            return holder.TrackId;
        }

        public override void BindView(View view, Context context, ICursor cursor)
        {
            var holder = (ViewHolder) view.Tag;

            int idColumn = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
            if (idColumn != -1)
            {
                holder.TrackId = cursor.GetLong(idColumn);
            }
            else
            {
                holder.TrackId = -1;
            }

            int titleColumn = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
            if (titleColumn != -1)
            {
                holder.TitleView.Text = cursor.GetString(titleColumn);
            }
            else
            {
                holder.TitleView.SetText(Resource.String.title_unknown_track);
            }

            int artistColumn = cursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
            if (artistColumn != -1)
            {
                holder.ArtistView.Text = cursor.GetString(artistColumn);
            }
            else
            {
                holder.ArtistView.SetText(Resource.String.title_unknown_artist);
            }

            if (ShouldDisplayCheckboxes)
            {
                holder.CheckBox.Visibility = ViewStates.Visible;
                holder.CheckBox.Checked = _callbacks.IsItemChecked(cursor.Position);
            }
            else
            {
                holder.CheckBox.Visibility = ViewStates.Gone;
            }
        }

        public override View NewView(Context context, ICursor cursor, ViewGroup parent)
        {
            var view = LayoutInflater.From(context).Inflate(Resource.Layout.View_TrackItem, parent, false);

            var holder = new ViewHolder();
            holder.TitleView = view.FindViewById<TextView>(Android.Resource.Id.Title);
            holder.ArtistView = view.FindViewById<TextView>(Resource.Id.artist);
            holder.CheckBox = view.FindViewById<CheckBox>(Android.Resource.Id.Checkbox);
            view.Tag = holder;

            return view;
        }
    }
}