using MediatR;
using Microsoft.EntityFrameworkCore;
using MockDataGenerator.Schools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Persistence.EF;

using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.GetTimetableUseCase;
using ModelTimetable = Szlem.Models.Schools.Timetable;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class GetTimetableUseCaseHandler : IRequestHandler<Query, Result>
    {
        private readonly AppDbContext _dbContext;

        public GetTimetableUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
        {
            var source = _dbContext.Set<ModelTimetable>().Include(x => x.School) as IQueryable<ModelTimetable>;

            ModelTimetable timetable = null;

            if (request.TimetableID.HasValue)
            {
                timetable = await source.SingleOrDefaultAsync(x => x.Id == request.TimetableID.Value);
            }
            else
            {
                timetable = await source
                    .Where(x => x.School.ID == request.SchoolID.Value)
                    .Where(x => x.ValidFrom <= request.ValidOn.Value)
                    .OrderByDescending(x => x.ValidFrom)
                    .FirstOrDefaultAsync();
            }

            if (timetable == null)
                throw new Exceptions.ResourceNotFoundException("Nie znaleziono planu zajęć");

            return Convert(timetable);
        }

        private Result Convert(ModelTimetable source)
        {
            return new Result() { Timetable = new TimetableConverter().Convert(source) };
        }
    }
}
