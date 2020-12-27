using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Szlem.Domain.Exceptions;

namespace Szlem.Engine.Exceptions
{
    public class InvalidRequestException : SzlemException
    {
        public InvalidRequestException()
        {
        }

        public InvalidRequestException(string message) : base(message)
        {
        }

        public InvalidRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
