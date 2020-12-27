using MediatR;
using MockDataGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.SharedKernel;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.GetEditionCooperationDetailsUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public class MockGetEditionCooperationDetailsUseCaseHandler : IRequestHandler<Query, Response>
    {
        private readonly ISzlemEngine _engine;

        public MockGetEditionCooperationDetailsUseCaseHandler(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var timetableResult = await _engine.Query(new GetTimetableUseCase.Query() { SchoolID = request.SchoolID, ValidOn = DateTime.Today });

            var courses = new[]
            {
                new CourseSummary()
                {
                    Class = "1a",
                    IsOngoing = new Random().Next() % 2 == 0,
                    Lecturers = new FullNameGenerator().GetNames(1, 3).ToArray()
                },
                new CourseSummary()
                {
                    Class = "1c",
                    IsOngoing = new Random().Next() % 2 == 0,
                    Lecturers = new FullNameGenerator().GetNames(1, 3).ToArray()
                }
            };

            var response = new Response()
            {
                SchoolID = 1,
                SchoolName = "I LO im. Mikołaja Kopernika",
                SchoolAddress = "Wałowa 7",
                SchoolCity = "Gdańsk",

                IsCurrentEdition = true,

                EditionID = 6,
                EditionName = "Edycja VI",
                EditionStartDate = DateTime.Today.AddMonths(-1),
                EditionEndDate = DateTime.Today.AddMonths(3).AddDays(17),

                LessonCount = 17,
                Courses = courses,
                Timetable = timetableResult.Timetable,
                CanStartNewCourse = true
            };

            return response;
        }
    }
}
