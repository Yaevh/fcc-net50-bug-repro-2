using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Stakeholders.RegionalCoordinator.Schools;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.AddBasicSchoolDataUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class AddBasicSchoolDataUseCaseHandler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dbContext;

        public AddBasicSchoolDataUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var schoolByName = await _dbContext.Set<Models.Schools.School>()
                .Where(x => EF.Functions.Like(request.SchoolName, x.Name) && EF.Functions.Like(request.City, x.City))
                .SingleOrDefaultAsync();
            if (schoolByName != null)
                throw new SchoolAlreadyExistsException("Szkoła o podanej nazwie już istnieje", schoolByName.ID);
            
            var school = new Models.Schools.School()
            {
                Name = request.SchoolName,
                City = request.City,
                Address = request.Address,
                Website = request.Website,
                ContactPhoneNumber = request.PhoneNumber,
                Email = request.Email
            };

            _dbContext.Add(school);

            return Unit.Value;
        }
    }
}
