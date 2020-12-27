using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class EditTimetableUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest
        {
            public Timetable Timetable { get; set; }
        }
        
        public class Validator : AbstractValidator<Command>
        {
            public Validator() : base()
            {
                RuleFor(x => x.Timetable).SetValidator(new Timetable.TimetableValidator());
                RuleFor(x => x.Timetable.ID).NotEmpty();
            }
        }
    }
}
