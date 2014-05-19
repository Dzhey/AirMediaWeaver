using System;

namespace AirMedia.Core.Controller.WebService.Model
{
    public struct PeerDescriptor
    {
        public string Guid { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastPing { get; set; }

        public override string ToString()
        {
            return string.Format("[PeerDescriptor(Guid: \"{0}\"; IpAddress: \"{1}\"; LastPing: \"{2}\")]",
                Guid, IpAddress, LastPing);
        }
    }
}