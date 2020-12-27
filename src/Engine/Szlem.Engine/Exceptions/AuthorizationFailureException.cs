using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Szlem.Domain.Exceptions;

namespace Szlem.Engine.Exceptions
{
    public class AuthorizationFailureException : SzlemException
    {
        public AuthorizationFailure Failure { get; }

        public AuthorizationFailureException()
        {
        }

        public AuthorizationFailureException(string message) : base(message)
        {
        }

        public AuthorizationFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AuthorizationFailureException(AuthorizationFailure failure) : base("You do not have permission to perform this action")
        {
            Failure = failure;
        }

        protected AuthorizationFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
