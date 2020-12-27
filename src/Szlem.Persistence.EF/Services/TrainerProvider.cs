using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Models.Users;
using Szlem.Recruitment.DependentServices;
using Szlem.SharedKernel;

namespace Szlem.Persistence.EF.Services
{
    public class TrainerProvider : ITrainerProvider
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TrainerProvider(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }


        public async Task<Maybe<TrainerDetails>> GetTrainerDetails(Guid guid)
        {
            var user = await _userManager.FindByIdAsync(guid.ToString());
            if (user == null)
                return Maybe<TrainerDetails>.None;
            else
                return Maybe<TrainerDetails>.From(new TrainerDetails() { Guid = user.Id, Name = user.ToString() });
        }

        public async Task<IReadOnlyCollection<TrainerDetails>> GetTrainerDetails(IReadOnlyCollection<Guid> guids)
        {
            var trainerMaybes = await guids.Distinct().SelectAsync(async guid => await GetTrainerDetails(guid));
            return trainerMaybes.Where(x => x.HasValue).Select(x => x.Value).ToArray();
        }
    }
}
