using System;

namespace AirMedia.Core.Requests.Model
{
	public class LoadRequestResult<TData> : RequestResult
    {
        public TData Data { get; set; }
	    private bool _isDisposed;

        public LoadRequestResult()
        {
        }

        public LoadRequestResult(int resultCode) : base(resultCode)
        {
        }

        public LoadRequestResult(int resultCode, TData resultData)
            : base(resultCode)
	    {
	        Data = resultData;
	    }

	    internal LoadRequestResult(int resultCode, TData resultData, Exception risenException) 
            : base(resultCode, risenException)
	    {
	        Data = resultData;
	    }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                var disposable = Data as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                    Data = default(TData);
                }
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}
