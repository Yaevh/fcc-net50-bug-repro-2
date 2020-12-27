using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class AddTimetableUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest
        {
            public int SchoolID { get; set; }
            
            public Timetable Timetable { get; set; }
        }
        
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolID).NotEmpty();
                RuleFor(x => x.Timetable).SetValidator(new Timetable.TimetableValidator());
            }
        }
    }
}
