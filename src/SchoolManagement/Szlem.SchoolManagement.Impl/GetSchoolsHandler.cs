using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using X.PagedList;

namespace Szlem.SchoolManagement.Impl
{
    internal class GetSchoolsHandler : IRequestHandler<GetSchools.Query, IPagedList<GetSchools.Summary>>
    {
        private readonly ISchoolRepository _repo;
        private readonly IClock _clock;
        public GetSchoolsHandler(ISchoolRepository repo, IClock clock)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<IPagedList<GetSchools.Summary>> Handle(GetSchools.Query request, CancellationToken cancellationToken)
        {
            var query = await _repo.GetAll(cancellationToken) as IEnumerable<SchoolReadModel>;
            if (string.IsNullOrEmpty(request.SearchPattern) == false)
            {
                query = query.Where(x =>
                    x.Name.Contains(request.SearchPattern, StringComparison.InvariantCultureIgnoreCase)
                    || x.City.Contains(request.SearchPattern, StringComparison.InvariantCultureIgnoreCase)
                    || x.Address.Contains(request.SearchPattern, StringComparison.InvariantCultureIgnoreCase));
            }

            return query
                .Select(x => new GetSchools.Summary() {
                    Id = x.Id.GetGuid(), Name = x.Name, Address = x.Address, City = x.City,
                    Status = x.GetEffectiveStatus(_clock.GetTodayDate())
                })
                .ToPagedList(request.PageNo, request.PageSize);
        }
    }
}
