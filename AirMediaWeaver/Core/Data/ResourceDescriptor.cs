using System;

namespace AirMedia.Core.Data
{
    public struct ResourceDescriptor
    {
        public string PublicGuid { get; set; }
        public long? LocalId { get; set; }
        public Uri Uri { get; set; }
    }
}