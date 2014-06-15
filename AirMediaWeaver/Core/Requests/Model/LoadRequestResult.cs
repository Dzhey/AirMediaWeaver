using System;

namespace AirMedia.Core.Requests.Model
{
	public class LoadRequestResult<TData> : RequestResult
    {
        public TData Data { get; set; }

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
    }
}
