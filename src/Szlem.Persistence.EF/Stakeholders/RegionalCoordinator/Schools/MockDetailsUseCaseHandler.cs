using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.DetailsUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class MockDetailsUseCaseHandler : IRequestHandler<Query, SchoolDetails>
    {
        private readonly AppDbContext _dbContext;
        private readonly Infrastructure.IRequestAuthorizationAnalyzer _authAnalyzer;

        public MockDetailsUseCaseHandler(
            AppDbContext dbContext,
            Infrastructure.IRequestAuthorizationAnalyzer requestAuthorizationAnalyzer)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _authAnalyzer = requestAuthorizationAnalyzer ?? throw new ArgumentNullException(nameof(requestAuthorizationAnalyzer));
        }

        public async Task<SchoolDetails> Handle(Query request, CancellationToken cancellationToken)
        {
            var school = await _dbContext.Set<Models.Schools.School>()
                .Include(x => x.ContactPersons)
                .Include(x => x.Timetables)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.ID == request.ID);

            if (school == null)
                throw new Exceptions.ResourceNotFoundException("Nie znaleziono szkoły");

            var uri = school.Website != null ? new Uri(school.Website) : null;

            var timetables = school.Timetables.Select(x => new TimetableConverter().Convert(x)).ToList();
            var currentTimetable = timetables.OrderBy(x => x.ValidFrom).LastOrDefault(x => x.ValidFrom <= DateTime.Today);

            var canAddTimetable = await _authAnalyzer.Authorize(new AddTimetableUseCase.Command() { SchoolID = request.ID });
            var canEditTimetable = currentTimetable != null && (await _authAnalyzer.Authorize(new EditTimetableUseCase.Command())).Succeeded;

            return new SchoolDetails()
            {
                ID = school.ID,
                Name = school.Name,
                Address = school.Address,
                City = school.City,
                Email = school.Email,
                PhoneNumber = school.ContactPhoneNumber,
                Website = uri,
                CanAddTimetable = canAddTimetable.Succeeded,
                CanEditTimetable = canEditTimetable,
                Contacts = school.ContactPersons.Select(x => new SchoolDetails.ContactPerson() {
                    ID = x.ID,
                    Name = x.Name,
                    Position = x.Position,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email
                }).ToList(),
                Editions = new[]
                {
                    new SchoolDetails.EditionSummary()
                    {
                        EditionID = 5,
                        EditionName = "Edycja V",
                        ClassCount = 2,
                        LessonCount = 12,
                        Lecturers = new[] { "Tom Araya", "Kerry King", "Jeff Hannemann", "Dave Lombardo" },
                        CanShowDetails = true
                    },
                    new SchoolDetails.EditionSummary()
                    {
                        EditionID = 6,
                        EditionName = "Edycja VI",
                        ClassCount = 3,
                        LessonCount = 18,
                        Lecturers = new[] { "Mateusz Morawiecki", "Beata Szydło", "Zbigniew Ziobro" },
                        CanShowDetails = true
                    }
                },
                Timetables = timetables,
                CurrentTimetable = currentTimetable,
            };
        }
    }
}
