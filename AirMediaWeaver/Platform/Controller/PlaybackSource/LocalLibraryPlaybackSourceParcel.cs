using AirMedia.Platform.Util;
using Android.OS;
using Java.Interop;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class LocalLibraryPlaybackSourceParcel : Java.Lang.Object, IParcelable
    {
        private static readonly GenericParcelableCreator<LocalLibraryPlaybackSourceParcel> Creator;

        public int CurrentPosition { get { return _currentPosition; } }
        public long[] TrackIds { get { return _trackIds; } }

        private readonly int _currentPosition;
        private readonly long[] _trackIds;

        static LocalLibraryPlaybackSourceParcel()
        {
            Creator = new GenericParcelableCreator<LocalLibraryPlaybackSourceParcel>(
                parcel => new LocalLibraryPlaybackSourceParcel(parcel));
        }

        [ExportField("CREATOR")]
        public static GenericParcelableCreator<LocalLibraryPlaybackSourceParcel> GetCreator()
        {
            return Creator;
        }

        public LocalLibraryPlaybackSourceParcel()
        {
        }

        public LocalLibraryPlaybackSourceParcel(Parcel parcel)
        {
            _currentPosition = parcel.ReadInt();

            int length = parcel.ReadInt();
            _trackIds = new long[length];

            parcel.ReadLongArray(_trackIds);
        }

        public LocalLibraryPlaybackSourceParcel(int currentPosition, long[] trackIds)
        {
            _currentPosition = currentPosition;
            _trackIds = trackIds;
        }

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteInt(_currentPosition);
            dest.WriteInt(_trackIds.Length);
            dest.WriteLongArray(_trackIds);
        }
    }
}