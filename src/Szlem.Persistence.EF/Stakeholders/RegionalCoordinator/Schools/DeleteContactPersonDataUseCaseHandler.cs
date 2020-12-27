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
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.DeleteContactPersonDataUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class DeleteContactPersonDataUseCaseHandler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dbContext;

        public DeleteContactPersonDataUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var contactPerson = await _dbContext.Set<Szlem.Models.Schools.ContactPerson>()
                .SingleOrDefaultAsync(x => x.ID == request.ID);
            
            if (contactPerson == null)
                throw new Exceptions.ResourceNotFoundException("Nie znaleziono osoby kontaktowej o podanym ID");

            _dbContext.Remove(contactPerson);

            return Unit.Value;
        }
    }
}
