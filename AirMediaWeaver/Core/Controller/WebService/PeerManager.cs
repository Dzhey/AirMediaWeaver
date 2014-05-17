using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;

namespace AirMedia.Core.Controller.WebService
{
    public class PeerManager
    {
        public static readonly string LogTag = typeof(PeerManager).Name;

        private readonly List<PeerDescriptor> _peers;
        private readonly string _selfGuid;

        public PeerManager(string selfGuid)
        {
            _peers = new List<PeerDescriptor>();
            _selfGuid = selfGuid;
        }

        public void UpdatePeers(AuthPacket packet)
        {
            if (packet.Guid == null)
            {
                AmwLog.Error(LogTag, "no peer guid specified in provided auth packet");
                return;
            }
            if (packet.Guid == _selfGuid)
            {
                AmwLog.Error(LogTag, "self peer guid specified in provided auth packet");
                return;
            }

            var peer = FindPeer(packet.Guid);
            if (peer == null)
            {
                peer = new PeerDescriptor();
                _peers.Add(peer);
            }
            peer.Guid = packet.Guid;
            peer.IpAddress = packet.IpAddress;
            peer.LastPing = DateTime.UtcNow;

            AmwLog.Debug(LogTag, string.Format("peer list updated; {0} peers available", _peers.Count));
        }

        public PeerDescriptor FindPeer(string peerGuid)
        {
            return _peers.FirstOrDefault(item => item.Guid == peerGuid);
        }

        public IReadOnlyCollection<PeerDescriptor> GetPeers()
        {
            return _peers.ToArray();
        }
    }
}