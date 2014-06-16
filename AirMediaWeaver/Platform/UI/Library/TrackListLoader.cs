
using Android.Content;
using Android.Provider;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.UI.Library
{
    public class TrackListLoader : Android.Support.V4.Content.CursorLoader
    {
        private static readonly Uri ContentUri = MediaStore.Audio.Media.ExternalContentUri;

        private static readonly string[] Projection = new[]
            {
                MediaStore.Audio.Media.InterfaceConsts.Id,
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.IsMusic
            };

        private static readonly string TrackSortOrder;

        static TrackListLoader()
        {
            const string sortByArtist = MediaStore.Audio.Media.InterfaceConsts.Artist + " ASC";
            const string sortByTitle = MediaStore.Audio.Media.InterfaceConsts.Title + " ASC";
            TrackSortOrder = string.Join(",", sortByArtist, sortByTitle);
        }

        public TrackListLoader(Context context)
            : this(context, ContentUri, Projection, 
            string.Format("{0}=1", MediaStore.Audio.Media.InterfaceConsts.IsMusic), null, TrackSortOrder)
        {
        }

        protected TrackListLoader(Context context, Uri uri, string[] projection, 
            string selection, string[] selectionArgs, string sortOrder)
            : base(context, uri, projection, selection, selectionArgs, sortOrder)
        {
        }
    }
}