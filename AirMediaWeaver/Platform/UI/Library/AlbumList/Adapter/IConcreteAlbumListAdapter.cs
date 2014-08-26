using System.Collections.Generic;


namespace AirMedia.Platform.UI.Library.AlbumList.Adapter
{
    public interface IConcreteAlbumListAdapter<T> : IAlbumListAdapter
    {
        void SetItems(IEnumerable<T> data);
    }
}