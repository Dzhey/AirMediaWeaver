using System;
using System.Runtime.Serialization;


namespace AirMedia.Core.Controller.WebService
{
    public class MulticastUdpServerException : Exception
    {
        public MulticastUdpServerException()
        {
        }

        public MulticastUdpServerException(string message) : base(message)
        {
        }

        protected MulticastUdpServerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MulticastUdpServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}