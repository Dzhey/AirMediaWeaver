using AirMedia.Platform.Util;
using Android.OS;
using Java.Interop;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class RemotePlaybackSourceParcel : Java.Lang.Object, IParcelable
    {
        private static readonly GenericParcelableCreator<RemotePlaybackSourceParcel> Creator;

        public int CurrentPosition { get { return _currentPosition; } }
        public string[] TrackGuids { get { return _trackGuids; } }

        private readonly int _currentPosition;
        private readonly string[] _trackGuids;

        static RemotePlaybackSourceParcel()
        {
            Creator = new GenericParcelableCreator<RemotePlaybackSourceParcel>(
                parcel => new RemotePlaybackSourceParcel(parcel));
        }

        [ExportField("CREATOR")]
        public static GenericParcelableCreator<RemotePlaybackSourceParcel> GetCreator()
        {
            return Creator;
        }

        public RemotePlaybackSourceParcel()
        {
        }

        public RemotePlaybackSourceParcel(Parcel parcel)
        {
            _currentPosition = parcel.ReadInt();

            int length = parcel.ReadInt();
            _trackGuids = new string[length];

            parcel.ReadStringArray(_trackGuids);
        }

        public RemotePlaybackSourceParcel(int currentPosition, string[] trackGuids)
        {
            _currentPosition = currentPosition;
            _trackGuids = trackGuids;
        }

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteInt(_currentPosition);
            dest.WriteInt(_trackGuids.Length);
            dest.WriteStringArray(_trackGuids);
        }
    }
}