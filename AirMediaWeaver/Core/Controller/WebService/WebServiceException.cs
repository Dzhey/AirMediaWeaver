using System;
using System.Runtime.Serialization;


namespace AirMedia.Core.Controller.WebService
{
    public class WebServiceException : Exception
    {
        public WebServiceException(string message) : base(message)
        {
        }

        public WebServiceException()
        {
        }

        protected WebServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public WebServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}