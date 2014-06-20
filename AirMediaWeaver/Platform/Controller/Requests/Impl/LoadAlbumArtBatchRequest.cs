using System;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class LoadAlbumArtBatchRequest : BatchRequest
    {
        private const int ArgsGcThreshold = 5;

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            if (Args.Length >= ArgsGcThreshold)
            {
                GC.Collect();
                GC.Collect();
            }

            return base.ExecuteImpl(out status);
        }
    }
}