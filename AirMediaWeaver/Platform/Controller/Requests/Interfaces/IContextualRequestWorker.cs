using System;
using AirMedia.Core.Requests.Abs;
using Android.OS;

namespace AirMedia.Platform.Controller.Requests.Interfaces
{
    public interface IContextualRequestWorker : IDisposable
    {
        bool IsResultHandlerDisabled { get; set; }
        void InitResultHandler();
        void InitUpdateHandler();
        void ResetResultHandler();
        void ResetUpdateHandler();
        AbsRequest PerformRequest();
        Bundle SaveState();
        void RestoreState(Bundle savedState);
    }
}