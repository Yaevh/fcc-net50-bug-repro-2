using CSharpFunctionalExtensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;

namespace Szlem.AspNetCore.Contracts.Identity
{
    public static class Register
    {
        public class Request : IRequest<Result>
        {
            public EmailAddress Email { get; set; }

            public string Password { get; set; }
        }
    }
}
