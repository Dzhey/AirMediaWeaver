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
        }

        public TrackListAdapter(Context context, ICursor c) 
            : base(context, c, CursorAdapterFlags.None)
        {
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
        }

        public override View NewView(Context context, ICursor cursor, ViewGroup parent)
        {
            var view = LayoutInflater.From(context).Inflate(Resource.Layout.View_TrackItem, parent, false);

            var holder = new ViewHolder();
            holder.TitleView = view.FindViewById<TextView>(Android.Resource.Id.Title);
            holder.ArtistView = view.FindViewById<TextView>(Resource.Id.artist);
            view.Tag = holder;

            return view;
        }
    }
}