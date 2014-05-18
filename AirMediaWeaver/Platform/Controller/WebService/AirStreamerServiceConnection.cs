using System;

using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Controller.WebService
{
    public class AirStreamerServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public event EventHandler<IAmwStreamerService> Connected;

        public bool IsConnected { get; private set; }

        public IAmwStreamerService Service
        {
            get { return _binder.Service; }
        }

        private AirStreamerBinder _binder;

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            IsConnected = true;
            _binder = (AirStreamerBinder) service;

            if (Connected != null)
            {
                Connected(this, Service);
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            IsConnected = false;
            _binder = null;
        }
    }
}