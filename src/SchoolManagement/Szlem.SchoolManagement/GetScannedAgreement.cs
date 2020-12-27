using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

#nullable enable
namespace Szlem.SchoolManagement
{
    public static class GetScannedAgreement
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Maybe<ScannedAgreement>>
        {
            public Guid SchoolId { get; set; }
            public Guid AgreementId { get; set; }
        }

        public class ScannedAgreement
        {
            public byte[] Content { get; set; } = Array.Empty<byte>();
            public string ContentType { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.AgreementId).NotEmpty();
                RuleFor(x => x.SchoolId).NotEmpty();
            }
        }
    }
}
