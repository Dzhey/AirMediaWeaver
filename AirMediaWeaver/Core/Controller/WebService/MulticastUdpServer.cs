using System;
using System.Net;
using System.Net.Sockets;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Logger;
using Socket = System.Net.Sockets.Socket;

namespace AirMedia.Core.Controller.WebService
{
    public class MulticastUdpServer : IDisposable, IMulticastSender, IMulticastReceiver
    {
        public static readonly string LogTag = typeof (MulticastUdpServer).Name;

        public bool IsStarted
        {
            get { return !_isStopped; }
        }

        public bool HasMulticastCapability
        {
            get
            {
                if (_sendSocket != null && _sendSocket.Connected)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsClientInitialized { get; private set; }

        public event EventHandler<AuthPacketReceivedEventArgs> AuthPacketReceived;

        public IPAddress ServiceAddress { get; private set; }

        private Socket _sendSocket;
        private Socket _recvSocket;
        private IPAddress _multicastAddress;
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

        public bool TryStart(int serviceAddress, bool initializeClientSocket)
        {
            if (IsStarted) Stop();

            IsClientInitialized = initializeClientSocket;

            ServiceAddress = new IPAddress(NetworkUtils.IpV4ToBytes(serviceAddress));
            _multicastAddress = IPAddress.Parse(Consts.DefaultMulticastAddress);
            _sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                            new MulticastOption(_multicastAddress, ServiceAddress));
                _sendSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.MulticastTimeToLive, Consts.DefaultMulticastTTL);
                _sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, false);

                _recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _recvSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                                            new MulticastOption(_multicastAddress, ServiceAddress));
                _recvSocket.Bind(new IPEndPoint(IPAddress.Any, Consts.DefaultMulticastPort));
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

            if (IsClientInitialized == false)
            {
                AmwLog.Warn(LogTag, "can't request multicast auth packet send" +
                                    ": client socket is not initialized");
                return;
            }

            string ipAddressString = ServiceAddress.ToString();
            _requestResultListener.SubmitRequest(new SendMulticastAuthRequest(this, ipAddressString), true);
        }

        public void Stop()
        {
            if (_isStopped) return;

            if (_sendSocket != null)
            {
                _sendSocket.Close();
                _sendSocket = null;
            }
            if (_recvSocket != null)
            {
                _recvSocket.Close();
                _recvSocket = null;
            }

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

            try
            {
                if (IsClientInitialized == false || _sendSocket.Connected == false)
                {
                    lock (_sendSocket)
                    {
                        if (_sendSocket.Connected == false)
                        {
                            _sendSocket.Connect(new IPEndPoint(_multicastAddress, Consts.DefaultMulticastPort));
                            AmwLog.Debug(LogTag, "client UDP socket initialized");
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                AmwLog.Warn(LogTag, "Unable to setup client socket to send multicast request", e.ToString());
                return 0;
            }

            if (IsClientInitialized == false)
            {
                AmwLog.Warn(LogTag, "can't send requested multicast message: client socket is not initialized");
                return 0;
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