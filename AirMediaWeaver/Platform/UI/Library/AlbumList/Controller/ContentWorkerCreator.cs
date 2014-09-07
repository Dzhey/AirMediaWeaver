using System;
using System.Collections.Generic;
using WorkerCreatorFunc = System.Func<AirMedia.Platform.UI.Library.AlbumList.Controller.IAlbumListContentWorkerCallbacks, AirMedia.Platform.UI.Library.AlbumList.Controller.IAlbumListContentWorker>;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public class ContentWorkerCreator
    {
        public const int AlbumListAppearanceGrid = 0;
        public const int AlbumListGroupingNone = 0;
        public const int AlbumListGroupingByArtist = 1;

        private class WorkerEntry
        {
            public int AlbumListAppearance { get; set; }
            public int AlbumListGrouping { get; set; }
            public WorkerCreatorFunc CreatorFunc { get; set; }
        }

        private static readonly List<WorkerEntry> Entries;

        static ContentWorkerCreator()
        {
            Entries = new List<WorkerEntry>();
            Entries.Add(new WorkerEntry
            {
                AlbumListAppearance = AlbumListAppearanceGrid, 
                AlbumListGrouping = AlbumListGroupingByArtist,
                CreatorFunc = CreateArtistsAlbumsGridWorker
            });
            Entries.Add(new WorkerEntry
            {
                AlbumListAppearance = AlbumListAppearanceGrid,
                AlbumListGrouping = AlbumListGroupingNone,
                CreatorFunc = CreateAlbumsGridWorker
            });
        }

        public IAlbumListContentWorker CreateContentWorker(int albumListAppearance,
            int albumListGrouping, IAlbumListContentWorkerCallbacks callbacks)
        {
            var entry = FindWorkerEntry(albumListAppearance, albumListGrouping);

            if (entry == null)
            {
                throw new ArgumentException("no content worker defined for specified content worker profile");
            }

            return entry.CreatorFunc(callbacks);
        }

        private WorkerEntry FindWorkerEntry(int albumListAppearance, int albumListGrouping)
        {
            foreach (var entry in Entries)
            {
                if (entry.AlbumListAppearance == albumListAppearance
                    && entry.AlbumListGrouping == albumListGrouping)
                {
                    return entry;
                }
            }

            return null;
        }

        private static IAlbumListContentWorker CreateArtistsAlbumsGridWorker(
            IAlbumListContentWorkerCallbacks callbacks)
        {
            return new AlbumsCategorizedGridWorker(callbacks);
        }

        private static IAlbumListContentWorker CreateAlbumsGridWorker(
            IAlbumListContentWorkerCallbacks callbacks)
        {
            return new AlbumsGridWorker(callbacks);
        }
    }
}