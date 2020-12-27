using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class InviteToTraining
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result>
        {
            public Guid EnrollmentId { get; set; }
            public PhoneNumber PhoneNumber { get; set; }
            public int? DeclaredTrainingID { get; set; }
        }
    }
}
