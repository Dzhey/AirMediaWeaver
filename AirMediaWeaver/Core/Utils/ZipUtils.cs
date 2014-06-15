using System.IO;
using System.IO.Compression;
using System.Text;


namespace AirMedia.Core.Utils
{
    public static class ZipUtils
    {
        public static byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static byte[] ZipString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            return Zip(bytes);
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return mso.ToArray();
            }
        }

        public static string UnzipString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(Unzip(bytes));
        }
    }
}