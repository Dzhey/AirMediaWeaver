using System;

namespace AirMedia.Core.Requests.Model
{
	public class LoadRequestResult<TData> : RequestResult
    {
        public TData ResultData { get; set; }

        public LoadRequestResult(int resultCode, TData resultData)
            : base(resultCode)
	    {
	        ResultData = resultData;
	    }

	    internal LoadRequestResult(int resultCode, TData resultData, Exception risenException) 
            : base(resultCode, risenException)
	    {
	        ResultData = resultData;
	    }
    }
}
