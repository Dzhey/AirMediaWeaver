using System;
using System.Collections.Generic;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Model
{
    public class BatchRequestResult : RequestResult
    {
        public KeyValuePair<AbsRequest, RequestResult>[] Results { get; set; }

        private bool _isDisposed;

        protected BatchRequestResult()
        {
        }

        public BatchRequestResult(int resultCode) : base(resultCode)
        {
        }

        internal BatchRequestResult(int resultCode, Exception risenException) : base(resultCode, risenException)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                foreach (var result in Results)
                {
                    result.Key.Dispose();
                    result.Value.Dispose();
                }

                Results = null;
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}