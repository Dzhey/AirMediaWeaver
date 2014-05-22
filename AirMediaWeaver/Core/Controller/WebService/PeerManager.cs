using System;
using System.Collections.Generic;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Logger;

namespace AirMedia.Core.Controller.WebService
{
    public abstract class PeerManager : IDisposable
    {
        public static readonly string LogTag = typeof(PeerManager).Name;

        protected RequestResultListener RequestListener
        {
            get { return _requestResultListener; }
        }

        private readonly ISet<string> _discoveredPeers;
        private readonly string _selfGuid;
        private PeersDao _peersDao;
        private RequestResultListener _requestResultListener;
        private bool _isDisposed;

        protected PeerManager(string selfGuid, PeersDao peersDao)
        {
            _selfGuid = selfGuid;
            _peersDao = peersDao;
            _discoveredPeers = new HashSet<string>();
            _requestResultListener = RequestResultListener.NewInstance(typeof(PeerManager).Name);
        }

        public void UpdatePeersAsync(AuthPacket packet)
        {
            var request = new SimpleRequest<AuthPacket>(packet, UpdatePeersAsyncImpl);
            _requestResultListener.SubmitRequest(request);
        }

        public bool UpdatePeers(AuthPacket packet)
        {
            if (packet.Guid == null)
            {
                AmwLog.Error(LogTag, "no peer guid specified in provided auth packet");
                return false;
            }
            /*if (packet.Guid == _selfGuid)
            {
                AmwLog.Error(LogTag, "self peer guid specified in provided auth packet");
                return false;
            }*/

            bool isNewPeer = _discoveredPeers.Contains(packet.Guid) == false;
            var peer = (PeerRecord) FindPeer(packet.Guid);

            if (peer == null)
            {
                peer = new PeerRecord();
            }

            peer.PeerGuid = packet.Guid;
            peer.Address = packet.IpAddress;
            peer.LastPing = DateTime.UtcNow;
            _peersDao.UpdatePeers(peer);

            _discoveredPeers.Add(peer.PeerGuid);

            if (isNewPeer)
            {
                OnNewPeerDiscovered(peer);
            }
            else
            {
                OnPeerUpdated(peer);
            }

            return true;
        }

        public IPeerDescriptor FindPeer(string peerGuid)
        {
            return _peersDao.QueryForGuid(peerGuid);
        }

        public IReadOnlyCollection<IPeerDescriptor> GetPeers()
        {
            return _peersDao.GetAll().ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void OnPeerUpdated(PeerRecord peer)
        {
            AmwLog.Debug(LogTag, string.Format("peer updated; peer: {0}", peer));
        }

        protected virtual void OnNewPeerDiscovered(PeerRecord peer)
        {
            AmwLog.Info(LogTag, string.Format("new peer discovered: \"{0}\"", peer));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _requestResultListener.Dispose();
                _requestResultListener = null;
                _peersDao = null;
            }

            _isDisposed = true;
        }

        private RequestResult UpdatePeersAsyncImpl(AuthPacket packet)
        {
            return new RequestResult(UpdatePeers(packet)
                                         ? RequestResult.ResultCodeOk
                                         : RequestResult.ResultCodeFailed);
        }
    }
}