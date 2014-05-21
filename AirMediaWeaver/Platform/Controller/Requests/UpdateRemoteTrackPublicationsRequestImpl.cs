using System;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Receivers;
using Android.Content;

namespace AirMedia.Platform.Controller.Requests
{
    public class UpdateRemoteTrackPublicationsRequestImpl : UpdateRemoteTrackPublicationsRequest
    {
        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var result = base.ExecuteImpl(out status);

            var updateIntent = new Intent(RemoteTrackPublicationsUpdateReceiver.ActionRemoteTrackPublicationsUpdated);
            updateIntent.SetPackage(App.Instance.PackageName);
            App.Instance.SendBroadcast(updateIntent);
            AmwLog.Debug(LogTag, "update remote track publications broadcast sent");

            return result;
        }

        protected override IRemoteTrackMetadata[] DownloadRemoteTrackPublications()
        {
            var downloadRequest = new DownloadBaseTracksInfoRequestImpl();
            var result = downloadRequest.Execute() as DownloadBaseTracksInfoRequest.RequestResult;

            if (downloadRequest.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "can't retrieve track publications: " +
                                     "error while downloading publications");

                return new IRemoteTrackMetadata[0];
            }

            if (result == null)
            {
                AmwLog.Error(LogTag, string.Format("obtained unexpected download track publications result"));

                return new IRemoteTrackMetadata[0];
            }

            return Array.ConvertAll(result.TrackInfo, input => (IRemoteTrackMetadata)input);
        }
    }
}