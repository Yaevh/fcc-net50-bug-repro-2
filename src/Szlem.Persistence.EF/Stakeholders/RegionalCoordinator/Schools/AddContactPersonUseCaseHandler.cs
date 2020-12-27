using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Models.Schools;
using Szlem.Persistence.EF;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.AddContactPersonDataUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class AddContactPersonUseCaseHandler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dbContext;

        public AddContactPersonUseCaseHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }


        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            var school = await _dbContext.Set<School>()
                .Include(x => x.ContactPersons)
                .SingleOrDefaultAsync(x => x.ID == request.SchoolID);
            if (school == null)
                throw new Exceptions.ResourceNotFoundException("Nie znaleziono szkoły o podanym ID");

            if (school.ContactPersons.Any(x => x.Email == request.Email))
                throw new Domain.Exceptions.ValidationException(nameof(request.Email), "Osoba kontaktowa o podanym adresie e-mail już istnieje");

            if (school.ContactPersons.Any(x => x.Name == request.Name))
                throw new Domain.Exceptions.ValidationException(nameof(request.Name), "Osoba kontaktowa o podanym nazwisku już istnieje");

            if (school.ContactPersons.Any(x => x.PhoneNumber == request.PhoneNumber))
                throw new Domain.Exceptions.ValidationException(nameof(request.PhoneNumber), "Osoba kontaktowa o podanym numerze telefonu już istnieje");

            school.AddContactPerson(new ContactPerson()
            {
                Name = request.Name,
                Position = request.Position,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber
            });

            return Unit.Value;
        }
    }
}
