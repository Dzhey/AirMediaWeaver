using System.IO;
using System.Net.Http.Headers;
using AirMedia.Core.Log;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.Controller.WebService.Http
{
    public class HttpResponseStreamWriter
    {
        private static readonly string LogTag = typeof(HttpResponseStreamWriter).Name;

        private readonly Stream _input;
        private readonly Stream _output;
        private readonly ContentRangeHeaderValue _rangeHeader;
        private readonly long _contentLength;

        public static HttpResponseStreamWriter NewInstance(long contentLength, Stream output,
            Stream input, ContentRangeHeaderValue range)
        {
            return new HttpResponseStreamWriter(contentLength, output, input, range);
        }

        private HttpResponseStreamWriter(long contentLength, Stream output, Stream input,
            ContentRangeHeaderValue rangeHeader)
        {
            _input = input;
            _output = output;
            _rangeHeader = rangeHeader;
            _contentLength = contentLength;
        }

        public void Write()
        {
            var buffer = new byte[Consts.HttpStreamFileReadBufferSize];

            if (_rangeHeader == null)
            {
                WriteRange(0, _contentLength, buffer);
                return;
            }

            long from = _rangeHeader.From ?? 0;
            long to = _rangeHeader.To ?? _contentLength;

            long written = WriteRange(from, to, buffer);
            AmwLog.Debug(LogTag, string.Format("{0} bytes requested; {1} bytes written; ({2}.{3})", 
                to - from, written, from, to));
        }

        private long WriteRange(long from, long to, byte[] buffer)
        {
            if (from != 0)
            {
                long nSeek = _input.Seek(from, SeekOrigin.Begin);
                AmwLog.Debug(LogTag, string.Format("read stream position: {0}", nSeek));
            }

            long written = 0;
            while (from <= to)
            {
                int length = (int) (to - from);
                if (length > buffer.Length)
                {
                    length = buffer.Length;
                }

                int read = _input.Read(buffer, 0, length);

                if (read == 0) break;

               _output.Write(buffer, 0, read);

                from += read;
                written += read;
            }

            return written;
        }

        public ContentRangeHeaderValue ComputeContentLength()
        {
            if (_rangeHeader == null || _rangeHeader.HasRange == false)
            {
                return new ContentRangeHeaderValue(_contentLength > 0 ? _contentLength - 1 : _contentLength);
            }

            long from = _rangeHeader.From ?? 0;
            long to = _rangeHeader.To ?? (_contentLength > 0 ? _contentLength - 1 : _contentLength);

            if (_contentLength > 0 && to > _contentLength - 1)
            {
                to = _contentLength - 1;
            }

            return new ContentRangeHeaderValue(from, to, _contentLength);
        }
    }
}