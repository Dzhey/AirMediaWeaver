using System;

namespace AirMedia.Core.Controller.WebService.Model
{
    public class AuthPacketReceivedEventArgs : EventArgs
    {
        public AuthPacket Packet { get; set; }
    }
}