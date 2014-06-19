using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Model;
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
                result.Add(CreateItem(artist, albums));
            }

            status = RequestStatus.Ok;

            return new CachedLoadRequestResult<List<TItem>>(
                RequestResult.ResultCodeOk, result);
        }

        protected abstract TItem CreateItem(ArtistBaseModel artist, AlbumBaseModel[] albums);
    }
}