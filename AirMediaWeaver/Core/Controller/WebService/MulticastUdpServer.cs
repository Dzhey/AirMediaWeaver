using System;
using System.Net;
using System.Net.Sockets;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform;
using AirMedia.Platform.Logger;

namespace AirMedia.Core.Controller.WebService
{
    public class MulticastUdpServer : IDisposable, IMulticastSender, IMulticastReceiver
    {
        public static readonly string LogTag = typeof (MulticastUdpServer).Name;

        public bool IsStarted
        {
            get { return !_isStopped; }
        }

        public event EventHandler<AuthPacketReceivedEventArgs> AuthPacketReceived;

        public int ServiceAddress { get; private set; }

        private Socket _sendSocket;
        private Socket _recvSocket;
        private IPAddress _serverAddress;
        private IPEndPoint _multicastIpEndPoint;
        private IPEndPoint _recvIpEndPoint;
        private bool _isDisposed;
        private bool _isStopped; 
        private readonly RequestResultListener _requestResultListener;

        public MulticastUdpServer()
        {
            _isStopped = true;

            int random = new Random().Next(int.MaxValue);
            string listenerTag = string.Format("{0}_{1}", typeof(HttpServer).Name, random);
            _requestResultListener = new RequestResultListener(listenerTag);

            _requestResultListener.RegisterResultHandler(
                typeof(SendMulticastAuthRequest), OnSendAuhPacketRequestFinished);
            _requestResultListener.RegisterResultHandler(typeof(ReceiveMulticastAuthRequest),
                OnReceiveAuthPacketRequestFinished);
        }

        public bool TryStart(int serviceAddress)
        {
            ServiceAddress = serviceAddress;

            _sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _serverAddress = IPAddress.Parse(Consts.DefaultMulticastAddress);
                _sendSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.AddMembership, new MulticastOption(_serverAddress));
                _sendSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.MulticastTimeToLive, Consts.DefaultMulticastTTL);
                _sendSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.MulticastLoopback, false);

                _multicastIpEndPoint = new IPEndPoint(_serverAddress, Consts.DefaultMulticastPort);
                _sendSocket.Connect(_multicastIpEndPoint);

                _recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _recvIpEndPoint = new IPEndPoint(IPAddress.Any, Consts.DefaultMulticastPort);
                _recvSocket.Bind(_recvIpEndPoint);
                _recvSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                            new MulticastOption(_serverAddress, IPAddress.Any));
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, e, "Unable to setup UDP server; Message: \"{0}\"", e.Message);

                return false;
            }

            _isStopped = false;

            RequestMulticastAuthPacketSend();
            RequestReceiveAuthPacket();

            return true;
        }

        private void OnReceiveAuthPacketRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, "error receiving incoming udp message");
            }
            else
            {
                var data = ((LoadRequestResult<AuthPacket>) args.Result).Data;
                AmwLog.Debug(LogTag, "received udp message");
                if (AuthPacketReceived != null)
                {
                    AuthPacketReceived(this, new AuthPacketReceivedEventArgs { Packet = data });
                }
            }

            RequestReceiveAuthPacket();
        }

        private void OnSendAuhPacketRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "error sending auth packet");
            }

            App.MainHandler.PostDelayed(RequestMulticastAuthPacketSend,
                                        Consts.SendMulticastAuthIntervalMillis);
        }

        private void RequestReceiveAuthPacket()
        {
            AmwLog.Debug(LogTag, "auth packet receive requested");

            if (IsStarted == false)
            {
                AmwLog.Warn(LogTag, "can't request auth packet receive" +
                                    ": multicast server is stopped");
                return;
            }

            _requestResultListener.SubmitDedicatedRequest(new ReceiveMulticastAuthRequest(this));
        }

        private void RequestMulticastAuthPacketSend()
        {
            AmwLog.Debug(LogTag, "multicast auth packet send requested");

            if (IsStarted == false)
            {
                AmwLog.Warn(LogTag, "can't request multicast auth packet send" +
                                    ": multicast server is stopped");
                return;
            }

            string ipAddressString = new IPAddress(BitConverter.GetBytes(ServiceAddress)).ToString();
            _requestResultListener.SubmitRequest(new SendMulticastAuthRequest(this, ipAddressString), true);
        }

        public void Stop()
        {
            if (_isStopped) return;

            _sendSocket.Close();
            _recvSocket.Close();

            _isStopped = true;
        }

        public int Receive(byte[] buffer)
        {
            if (IsStarted == false)
            {
                throw new MulticastUdpServerException("can't receive data: multicast " +
                                                      "udp server is stopped");
            }

            return _recvSocket.Receive(buffer);
        }

        public int SendMulticast(byte[] data, int offset, int length)
        {
            if (IsStarted == false)
            {
                throw new MulticastUdpServerException("can't send data: multicast " +
                                                      "udp server is stopped");
            }

            return _sendSocket.Send(data, offset, length, SocketFlags.None);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                Stop();
                if (_recvSocket != null)
                {
                    _recvSocket.Dispose();
                    _recvSocket = null;
                }
                if (_sendSocket != null)
                {
                    _sendSocket.Dispose();
                    _sendSocket = null;
                }
                _requestResultListener.RemoveResultHandler(typeof(SendMulticastAuthRequest));
                _requestResultListener.RemoveResultHandler(typeof(ReceiveMulticastAuthRequest));
                _requestResultListener.Dispose();
            }

            _isDisposed = true;
        }
    }
}