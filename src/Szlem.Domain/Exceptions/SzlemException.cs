using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Szlem.Domain.Exceptions
{
    public class SzlemException : ApplicationException
    {
        public SzlemException()
        {
        }

        public SzlemException(string message) : base(message)
        {
        }

        public SzlemException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SzlemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
