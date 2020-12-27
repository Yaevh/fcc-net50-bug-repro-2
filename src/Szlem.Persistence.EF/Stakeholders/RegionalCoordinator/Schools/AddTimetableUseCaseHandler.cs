using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Models.Schools;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.AddTimetableUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class AddTimetableUseCaseHandler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dbContext;

        public AddTimetableUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var school = await _dbContext.Set<School>()
                .SingleOrDefaultAsync(x => x.ID == request.SchoolID);

            var timetable = new TimetableConverter().Convert(request.Timetable);
            timetable.School = school ?? throw new Domain.Exceptions.ValidationException(nameof(request.SchoolID), "Szkoła o podanym ID nie istnieje");

            _dbContext.Add(timetable);

            return Unit.Value;
        }
    }
}
