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
    public static class SendTrainingReminder
    {
        [AllowAnonymous]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid EnrollmentId { get; set; }
            public int TrainingId { get; set; }
        }
    }
}
