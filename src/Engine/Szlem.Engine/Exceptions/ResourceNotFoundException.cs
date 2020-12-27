using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Szlem.Domain.Exceptions;

namespace Szlem.Engine.Exceptions
{
    public class ResourceNotFoundException : SzlemException
    {
        public ResourceNotFoundException()
        {
        }

        public ResourceNotFoundException(string message) : base(message)
        {
        }

        public ResourceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
