using System;

namespace AirMedia.Core.Requests.Model
{
    public class RequestResult : IDisposable
    {
        public static readonly RequestResult ResultFailed;
        public static readonly RequestResult ResultOk;
        public static readonly RequestResult ResultCancelled;

        public const int ResultCodeOk = 1;
        public const int ResultCodeOkUpdate = 2;
        public const int ResultCodeFailed = -1;
        public const int ResultCodeCancelled = -2;

        public Exception RisenException { get; protected set; }
        public string ErrorMessage { get; set; }

        static RequestResult()
        {
            ResultFailed = new RequestResult(ResultCodeFailed);
            ResultOk = new RequestResult(ResultCodeOk);
            ResultCancelled = new RequestResult(ResultCodeCancelled);
        }

        public bool HasResultCode { get { return _resultCode.HasValue; } }

        public int ResultCode
        {
            get
            {
                if (_resultCode.HasValue == false)
                {
                    throw new InvalidOperationException("result code is undefined");
                }

                return _resultCode.Value;
            }

            protected set
            {
                if (_resultCode.HasValue)
                {
                    throw new InvalidOperationException("result code is already defined");
                }

                _resultCode = value;
            }
        }

        private int? _resultCode;
        private bool _isDisposed;

        protected RequestResult()
        {
        }

        public RequestResult(int resultCode)
        {
            ResultCode = resultCode;
        }

        internal RequestResult(int resultCode, Exception risenException)
        {
            ResultCode = resultCode;
            RisenException = risenException;
        }

        public override string ToString()
        {
            return string.Format("[{0}: " +
                                 "result-code:\"{1}\", " +
                                 "error-message:\"{2}\", " +
                                 "exception:\"{3}\"]", 
                                 GetType().Name, 
                                 ResultCode, 
                                 ErrorMessage,
                                 RisenException);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                RisenException = null;
                ErrorMessage = null;
            }

            _isDisposed = true;
        }
    }
}
