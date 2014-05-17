using System;

namespace AirMedia.Core.Controller.WebService.Model
{
    public class PeerDescriptor
    {
        public string Guid { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastPing { get; set; }
    }
}