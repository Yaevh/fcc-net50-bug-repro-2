using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class GetTimetableUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Result>
        {
            public int? SchoolID { get; set; }

            public DateTime? ValidOn { get; set; }

            public int? TimetableID { get; set; }
        }

        public class Result
        {
            public Timetable Timetable { get; set; }
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x).Must(x => x.TimetableID.HasValue || (x.SchoolID.HasValue && x.ValidOn.HasValue));

                When(x => x.TimetableID.HasValue, () => {
                    RuleFor(x => x.TimetableID).NotEmpty();
                    RuleFor(x => x.SchoolID).Empty();
                    RuleFor(x => x.ValidOn).Empty();
                });

                When(x => x.SchoolID.HasValue, () => {
                    RuleFor(x => x.TimetableID).Empty();
                    RuleFor(x => x.SchoolID).NotEmpty();
                    RuleFor(x => x.ValidOn).NotEmpty();
                });
            }
        }
    }
}
