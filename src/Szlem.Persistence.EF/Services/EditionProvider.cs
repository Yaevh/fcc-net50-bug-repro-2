using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;

namespace Szlem.Persistence.EF.Services
{
    public class EditionProvider : IEditionProvider
    {
        private readonly AppDbContext _dbContext;

        public EditionProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Maybe<EditionDetails>> GetEdition(int editionId)
        {
            var result = await _dbContext.Editions
                .AsNoTracking()
                .Where(x => x.ID == editionId)
                .Select(x => new { x.ID, x.StartDate, x.EndDate })
                .SingleOrDefaultAsync();

            if (result == null)
                return Maybe<EditionDetails>.None;

            return Maybe<EditionDetails>.From(new EditionDetails() {
                Id = result.ID,
                StartDateTime = NodaTime.LocalDateTime.FromDateTime(result.StartDate).InMainTimezone().ToInstant(),
                EndDateTime = NodaTime.LocalDateTime.FromDateTime(result.EndDate).InMainTimezone().ToInstant()
            });
        }
    }
}
