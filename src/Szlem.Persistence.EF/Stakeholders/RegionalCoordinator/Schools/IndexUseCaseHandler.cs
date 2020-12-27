using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.IndexUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class IndexUseCaseHandler : IRequestHandler<Query, IReadOnlyCollection<SchoolSummary>>
    {
        private readonly AppDbContext _dbContext;

        public IndexUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<IReadOnlyCollection<SchoolSummary>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _dbContext.Set<Models.Schools.School>()
                .AsNoTracking()
                .Select(x => new SchoolSummary()
                {
                    ID = x.ID,
                    Name = x.Name,
                    Address = x.Address,
                    City = x.City
                })
                .ToListAsync();
        }
    }
}
