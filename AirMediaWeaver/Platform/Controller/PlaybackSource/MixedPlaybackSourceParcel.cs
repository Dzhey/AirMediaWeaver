using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Platform.Util;
using Android.OS;
using Java.Interop;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class MixedPlaybackSourceParcel : Java.Lang.Object, IParcelable
    {
        private static readonly GenericParcelableCreator<MixedPlaybackSourceParcel> Creator;

        public int EncodedPosition { get { return _position; } }

        public long[] TrackIds { get { return _trackIds; } }
        public string[] TrackGuids { get { return _trackGuids; } }
        public int[] Positions { get { return _positions; } }

        private readonly int _position;
        private readonly long[] _trackIds;
        private readonly string[] _trackGuids;
        private readonly int[] _positions;

        static MixedPlaybackSourceParcel()
        {
            Creator = new GenericParcelableCreator<MixedPlaybackSourceParcel>(
                parcel => new MixedPlaybackSourceParcel(parcel));
        }

        [ExportField("CREATOR")]
        public static GenericParcelableCreator<MixedPlaybackSourceParcel> GetCreator()
        {
            return Creator;
        }

        public MixedPlaybackSourceParcel()
        {
        }

        public MixedPlaybackSourceParcel(Parcel parcel)
        {
            _position = parcel.ReadInt();

            int length = parcel.ReadInt();
            _trackIds = new long[length];
            parcel.ReadLongArray(_trackIds);

            length = parcel.ReadInt();
            _trackGuids = new string[length];
            parcel.ReadStringArray(_trackGuids);

            length = parcel.ReadInt();
            _positions = new int[length];
            parcel.ReadIntArray(_positions);
        }

        public MixedPlaybackSourceParcel(int position, IEnumerable<ITrackSourceDefinition> tracks)
        {
            _position = position;
            var trackIds = new List<long>();
            var trackGuids = new List<string>();
            var positions = new List<int>();

            foreach (var track in tracks)
            {
                if (track.TrackId != 0)
                {
                    positions.Add(trackIds.Count);
                    trackIds.Add(track.TrackId);
                }
                else
                {
                    positions.Add(trackGuids.Count);
                    trackGuids.Add(track.TrackGuid);
                }
            }

            _trackIds = trackIds.ToArray();
            _trackGuids = trackGuids.ToArray();
            _positions = positions.ToArray();
        }

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteInt(_position);
            dest.WriteInt(_trackIds.Length);
            dest.WriteLongArray(_trackIds);
            dest.WriteInt(_trackGuids.Length);
            dest.WriteStringArray(_trackGuids);
            dest.WriteInt(_positions.Length);
            dest.WriteIntArray(_positions);
        }
    }
}