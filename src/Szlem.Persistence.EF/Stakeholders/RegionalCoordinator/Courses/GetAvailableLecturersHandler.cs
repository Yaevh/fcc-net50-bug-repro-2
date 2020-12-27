using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Persistence.EF;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Courses
{
    internal class GetAvailableLecturersHandler : IRequestHandler<GetAvailableLecturers.Query, GetAvailableLecturers.Response>
    {
        private readonly AppDbContext _dbContext;

        public GetAvailableLecturersHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<GetAvailableLecturers.Response> Handle(GetAvailableLecturers.Query request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
