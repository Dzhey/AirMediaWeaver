using System;


namespace AirMedia.Core.Data.Model
{
    public interface IPeerDescriptor
    {
        string PeerGuid { get; set; }
        string Address { get; set; }
        DateTime LastPing { get; set; }
    }
}