using System;
using System.Net.Http.Headers;


namespace AirMedia.Platform.Controller.WebService.Http
{
    public static class HttpContentRangeParser
    {
        public static bool TryParseRange(string input, long contentLength, out ContentRangeHeaderValue value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(input)) return false;

            if (input.IndexOf(',') > -1 
                || input.IndexOf('-') == -1 
                || input.IndexOf('=') == -1) return false;

            string[] split = input.Split(new[] { Convert.ToChar("=") }, 2, StringSplitOptions.RemoveEmptyEntries);
            string range = split[1];

            long start;
            long end;

            if (range.StartsWith("-"))
            {
                // The n-number of the last bytes is requested
                start = contentLength - Convert.ToInt64(range.Substring(1));
                end = contentLength;
            }
            else
            {
                split = range.Split(new[] { Convert.ToChar("-") });
                start = Convert.ToInt64(split[0]);
                long temp = 0;
                end = (split.Length > 1 && Int64.TryParse(split[1], out temp)) ? Convert.ToInt64(split[1]) : contentLength;
            }

            end = Math.Min(end, contentLength);

            value = new ContentRangeHeaderValue(start, end);

            return true;
        }

    }
}