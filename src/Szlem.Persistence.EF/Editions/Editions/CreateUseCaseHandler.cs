using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Editions.Editions;
using Szlem.Models.Editions;

namespace Szlem.Persistence.EF.Editions.Editions
{
    internal class CreateUseCaseHandler : IRequestHandler<CreateUseCase.Command, Result<CreateUseCase.Response, Domain.Error>>
    {
        private readonly AppDbContext _dbContext;

        public CreateUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Result<CreateUseCase.Response, Domain.Error>> Handle(CreateUseCase.Command request, CancellationToken cancellationToken)
        {
            var editions = await _dbContext.Editions.AsNoTracking().ToListAsync();
            var dateIntervals = editions.Select(x => ToInterval(x.StartDate, x.EndDate)).ToList();
            var requestInterval = ToInterval(request.StartDate, request.EndDate);

            if (dateIntervals.Any(x => x.Overlaps(requestInterval)))
                return Result.Failure<CreateUseCase.Response, Domain.Error>(new Domain.Error.DomainError("New Edition overlaps old editions"));

            var edition = new Edition()
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Name = request.Name
            };
            edition.CumulativeStatistics.CityCount = request.CumulativeStatistics.CityCount;
            edition.CumulativeStatistics.LecturerCount = request.CumulativeStatistics.LecturerCount;
            edition.CumulativeStatistics.LessonCount = request.CumulativeStatistics.LessonCount;
            edition.CumulativeStatistics.SchoolCount = request.CumulativeStatistics.SchoolCount;
            edition.CumulativeStatistics.StudentCount = request.CumulativeStatistics.StudentCount;

            _dbContext.Set<Edition>().Add(edition);

            var rows = await _dbContext.SaveChangesAsync();
            return Result.Success<CreateUseCase.Response, Domain.Error>(new CreateUseCase.Response() { Id = edition.ID });
        }

        private DateInterval ToInterval(DateTime start, DateTime end)
        {
            return new DateInterval(
                new LocalDate(start.Year, start.Month, start.Day),
                new LocalDate(end.Year, end.Month, end.Day));
        }
    }
}
