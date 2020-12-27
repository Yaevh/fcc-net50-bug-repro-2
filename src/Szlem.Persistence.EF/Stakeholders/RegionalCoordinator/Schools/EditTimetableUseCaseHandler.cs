using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Models.Schools;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.EditTimetableUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class EditTimetableUseCaseHandler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dbContext;

        public EditTimetableUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var oldTimetable = await _dbContext.Set<Szlem.Models.Schools.Timetable>()
                .Include(x => x.School)
                .SingleOrDefaultAsync(x => x.Id == request.Timetable.ID);
            if (oldTimetable == null)
                throw new Exceptions.InvalidRequestException("Nie znaleziono planu zajęć o podanym ID");

            var newTimetable = new TimetableConverter().Convert(request.Timetable);
            newTimetable.School = oldTimetable.School;

            _dbContext.Remove(oldTimetable);
            _dbContext.Add(newTimetable);

            return Unit.Value;
        }
    }
}
