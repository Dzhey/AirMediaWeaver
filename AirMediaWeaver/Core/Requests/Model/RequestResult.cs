﻿using System;

namespace AirMedia.Core.Requests.Model
{
    public class RequestResult
    {
        public static readonly RequestResult ResultFailed;
        public static readonly RequestResult ResultOk;

        public const int ResultCodeOk = 1;
        public const int ResultCodeOkUpdate = 2;
        public const int ResultCodeFailed = -1;
        public const int ResultCodeCancelled = -2;

        public Exception RisenException { get; private set; }
        public string ErrorMessage { get; set; }

        static RequestResult()
        {
            ResultFailed = new RequestResult(ResultCodeFailed);
            ResultOk = new RequestResult(ResultCodeOk);
        }

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

            private set
            {
                if (_resultCode.HasValue)
                {
                    throw new InvalidOperationException("result code is already defined");
                }

                _resultCode = value;
            }
        }

        private int? _resultCode;

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
    }
}