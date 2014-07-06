using System.Collections.Generic;
using AirMedia.Core.Controller.Encodings;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public abstract class AbsLoadLocalArtistAlbumsRequest<TItem> : BaseLoadCachedRequest<List<TItem>>
    {
        private readonly ITrackMetadataDao _dao;

        protected AbsLoadLocalArtistAlbumsRequest(IRequestResultCache cache, 
            ITrackMetadataDao dao) : base(cache)
        {
            _dao = dao;
        }

        protected override string GetCacheKey()
        {
            return "LoadLocalArtistAlbumsRequest_cache_key";
        }

        protected override CachedLoadRequestResult<List<TItem>> DoLoad(out RequestStatus status)
        {
            var result = new List<TItem>();

            var artists = _dao.QueryForLocalArtists();

            foreach (var artist in artists)
            {
                var albums = _dao.QueryForArtistAlbums(artist.ArtistId);

                for (int i = 0; i < albums.Length; i++)
                {
                    string albumName = albums[i].AlbumName;

                    if (EncodingHelper.CheckIsMalformedText(albumName) == false)
                        continue;

                    bool converted = EncodingHelper.TryConvertText(albumName, out albumName);
                    if (converted)
                    {
                        string msg = string.Format("malformed album name converted: " +
                                                   "\"{0}\" -> \"{1}\"", albums[i].AlbumName, albumName);
                        AmwLog.Debug(LogTag, msg);
                    }
                    else
                    {
                        AmwLog.Warn(LogTag, string.Format("malformed album name \"{0}\" " +
                                                          "not converted", albumName));
                    }

                    albums[i].AlbumName = albumName;
                }

                result.Add(CreateItem(artist, albums));
            }

            status = RequestStatus.Ok;

            return new CachedLoadRequestResult<List<TItem>>(
                RequestResult.ResultCodeOk, result);
        }

        protected abstract TItem CreateItem(ArtistBaseModel artist, AlbumBaseModel[] albums);
    }
}