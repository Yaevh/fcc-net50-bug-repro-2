using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Editions.Editions;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.Interfaces;
using Szlem.Models.Editions;

namespace Szlem.Persistence.EF.Editions.Editions
{
    public class IndexUseCaseHandler : IRequestHandler<IndexUseCase.Query, IndexUseCase.EditionSummary[]>
    {
        private readonly AppDbContext _dbContext;
        private readonly IRequestAuthorizationAnalyzer _authorizationAnalyzer;

        public IndexUseCaseHandler(AppDbContext dbContext, IRequestAuthorizationAnalyzer authorizationAnalyzer)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _authorizationAnalyzer = authorizationAnalyzer ?? throw new ArgumentNullException(nameof(authorizationAnalyzer));
        }

        public async Task<IndexUseCase.EditionSummary[]> Handle(IndexUseCase.Query request, CancellationToken cancellationToken)
        {
            var editions = await _dbContext.Editions.AsNoTracking().ToListAsync();
            
            return await Task.WhenAll(editions
                .Select(async x => new IndexUseCase.EditionSummary()
                {
                    ID = x.ID,
                    Name = x.Name,
                    StartDate = NodaTime.LocalDate.FromDateTime(x.StartDate),
                    EndDate = NodaTime.LocalDate.FromDateTime(x.EndDate),
                    CanShowDetails = (await _authorizationAnalyzer.Authorize(new DetailsUseCase.Query() { EditionID = x.ID })).Succeeded
                }));
        }
    }
}
