using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Domain.Exceptions
{
    public class AggregateMismatchException : SzlemException
    {
        public AggregateMismatchException(string message) : base(message) { }

        public AggregateMismatchException(string message, Exception innerException) : base(message, innerException) { }
    }
}
