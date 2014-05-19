using System;


namespace AirMedia.Core.Data
{
    public interface IAmwContentProvider
    {
        Uri CreateRemoteTrackUri(string address, string port, string trackGuid);
    }
}