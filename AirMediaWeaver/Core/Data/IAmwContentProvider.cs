using System;
using AirMedia.Core.Data.Model;


namespace AirMedia.Core.Data
{
    public interface IAmwContentProvider
    {
        Uri CreateRemoteTrackUri(string address, string port, string trackGuid);
        Uri CreateTrackDownloadDestinationUri(IRemoteTrackMetadata metadata);
        Uri CreatePutAuthPacketUri(string address, string port);
    }
}