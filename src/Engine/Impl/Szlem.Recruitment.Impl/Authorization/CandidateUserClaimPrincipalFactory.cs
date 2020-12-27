using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Models.Users;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.Recruitment.Impl.Authorization
{
    internal class CandidateUserClaimPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _baseImpl;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ICampaignRepository _campaignRepo;
        private readonly IEditionProvider _editionProvider;
        private readonly NodaTime.IClock _clock;

        public CandidateUserClaimPrincipalFactory(
            IUserClaimsPrincipalFactory<ApplicationUser> baseImpl,
            IEnrollmentRepository enrollmentRepo,
            ICampaignRepository campaignRepo,
            IEditionProvider editionProvider,
            NodaTime.IClock clock)
        {
            _baseImpl = baseImpl ?? throw new ArgumentNullException(nameof(baseImpl));
            _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
            _campaignRepo = campaignRepo ?? throw new ArgumentNullException(nameof(campaignRepo));
            _editionProvider = editionProvider ?? throw new ArgumentNullException(nameof(editionProvider));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }


        public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await _baseImpl.CreateAsync(user);
            if (user.EmailConfirmed == false)
                return principal;
            
            var email = EmailAddress.Parse(user.Email);
            var submissions = _enrollmentRepo.Query().Where(x => x.Email == email);
            var filteredSubmissions = submissions
                .Where(x => x.Email.ToString().ToLowerInvariant() == user.Email.ToLowerInvariant())
                .ToArray();
            if (filteredSubmissions.None())
                return principal;

            var latestSubmission = filteredSubmissions.OrderByDescending(x => x.Timestamp).First();
            var campaign = await _campaignRepo.GetById(latestSubmission.Campaign.Id);
            var edition = await _editionProvider.GetEdition(campaign.EditionId);

            if (edition.HasNoValue)
                throw new ApplicationException($"Niespójność danych: nie znaleziono edycji o ID={campaign.EditionId} dla kampanii o ID={campaign.Id}");

            if (new NodaTime.Interval(edition.Value.StartDateTime, edition.Value.EndDateTime).Contains(_clock.GetCurrentInstant()) == false)
                return principal;

            principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(SharedKernel.ClaimTypes.Candidate, latestSubmission.Id.GetGuid().ToString()) }));
            return principal;
        }
    }
}
