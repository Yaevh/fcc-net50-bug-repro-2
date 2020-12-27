using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
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
using Szlem.Recruitment.Impl;
using Szlem.Recruitment.Impl.Authorization;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using X.PagedList;
using Xunit;

namespace Szlem.Recruitment.Tests.Authorization
{
    public class CandidateUserClaimPrincipalFactoryTests
    {
        [Fact(DisplayName = "Jeśli potwierdzony e-mail użytkownika jest skorelowany z formularzem rekrutacyjnym z bieżącej edycji, to wygenerowany ClaimsPrincipal ma claim Candidate z GUIDem odpowiadającym EnrollmentId")]
        public async Task If_user_has_confirmed_email_that_can_be_correlated_to_recruitment_form_from_current_edition_then_factory_creates_ClaimsPrincipal_with_Candidate_claim()
        {
            // Arrange
            var clock = NodaTime.SystemClock.Instance;
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;

            var user = new ApplicationUser() { Email = "andrzej@strzelba.com", EmailConfirmed = true };

            var userClaimPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(f => f.CreateAsync(It.IsAny<ApplicationUser>()) == Task.FromResult(new ClaimsPrincipal()));
            var enrollment = new EnrollmentReadModel() { Id = enrollmentId, Email = EmailAddress.Parse("ANDRZEJ@STRZELBA.COM"), Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 } };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                handler => handler.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict
            );

            var campaign = new Campaign(
                startDateTime: clock.GetOffsetDateTime().Minus(NodaTime.Duration.FromDays(7)),
                endDateTime: clock.GetOffsetDateTime().Plus(NodaTime.Duration.FromDays(7)),
                editionId: 1);
            var campaignRepo = Mock.Of<ICampaignRepository>(repo => repo.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);

            var edition = new EditionDetails() { StartDateTime = clock.GetCurrentInstant().Minus(NodaTime.Duration.FromDays(30)), EndDateTime = clock.GetCurrentInstant().Plus(NodaTime.Duration.FromDays(30)) };
            var editionRepo = Mock.Of<IEditionProvider>(provider => provider.GetEdition(1) == Task.FromResult(Maybe<EditionDetails>.From(edition)), MockBehavior.Strict);

            var principalFactory = new CandidateUserClaimPrincipalFactory(userClaimPrincipalFactory, enrollmentRepo, campaignRepo, editionRepo, clock);

            // Act
            var principal = await principalFactory.CreateAsync(user);

            //Assert
            var claim = Assert.Single(principal.Claims, claim => claim.Type == SharedKernel.ClaimTypes.Candidate);
            Assert.Equal(enrollmentId.GetGuid().ToString(), claim.Value);
        }

        [Fact(DisplayName = "Jeśli e-mail użytkownika nie jest potwierdzony, to wygenerowany ClaimsPrincipal nie ma claima Candidate")]
        public async Task If_user_does_not_have_confirmed_email_then_factory_creates_ClaimsPrincipal_without_Candidate_claim()
        {
            // Arrange
            var clock = NodaTime.SystemClock.Instance;

            var user = new ApplicationUser() { Email = "andrzej@strzelba.com", EmailConfirmed = false };

            var userClaimPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(f => f.CreateAsync(It.IsAny<ApplicationUser>()) == Task.FromResult(new ClaimsPrincipal()));
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(MockBehavior.Strict);
            var campaignRepo = Mock.Of<ICampaignRepository>(MockBehavior.Strict);
            var editionRepo = Mock.Of<IEditionProvider>(MockBehavior.Strict);

            var principalFactory = new CandidateUserClaimPrincipalFactory(userClaimPrincipalFactory, enrollmentRepo, campaignRepo, editionRepo, clock);

            // Act
            var principal = await principalFactory.CreateAsync(user);

            //Assert
            Assert.DoesNotContain(principal.Claims, claim => claim.Type == SharedKernel.ClaimTypes.Candidate);
        }

        [Fact(DisplayName = "Jeśli z e-mailem użytownika nie jest skorelowany formularz rekrutacyjny, to wygenerowany ClaimsPrincipal nie ma claima Candidate")]
        public async Task If_user_has_confirmed_email_that_cannot_be_correlated_to_recruitment_form_then_factory_creates_ClaimsPrincipal_without_Candidate_claim()
        {
            // Arrange
            var clock = NodaTime.SystemClock.Instance;

            var user = new ApplicationUser() { Email = "andrzej@strzelba.com", EmailConfirmed = true };

            var userClaimPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(f => f.CreateAsync(It.IsAny<ApplicationUser>()) == Task.FromResult(new ClaimsPrincipal()));
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                handler => handler.Query() == Array.Empty<EnrollmentReadModel>().AsQueryable(),
                MockBehavior.Strict
            );
            var campaignRepo = Mock.Of<ICampaignRepository>(MockBehavior.Strict);
            var editionRepo = Mock.Of<IEditionProvider>(MockBehavior.Strict);

            var principalFactory = new CandidateUserClaimPrincipalFactory(userClaimPrincipalFactory, enrollmentRepo, campaignRepo, editionRepo, clock);

            // Act
            var principal = await principalFactory.CreateAsync(user);

            //Assert
            Assert.DoesNotContain(principal.Claims, claim => claim.Type == SharedKernel.ClaimTypes.Candidate);
        }

        [Fact(DisplayName = "Jeśli z e-mailem użytkownika jest skorelowany formularz rekrutacyjny z innej edycji, to wygenerowany ClaimsPrincipal nie ma claima Candidate")]
        public async Task If_user_has_confirmed_email_that_can_be_correlated_to_recruitment_form_from_another_edition_then_factory_creates_ClaimsPrincipal_without_Candidate_claim()
        {
            // Arrange
            var clock = NodaTime.SystemClock.Instance;

            var user = new ApplicationUser() { Email = "andrzej@strzelba.com", EmailConfirmed = true };

            var userClaimPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(f => f.CreateAsync(It.IsAny<ApplicationUser>()) == Task.FromResult(new ClaimsPrincipal()));
            var enrollment = new EnrollmentReadModel() { Email = EmailAddress.Parse("ANDRZEJ@STRZELBA.COM"), Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 } };

            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                handler => handler.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict
            );

            var campaign = new Campaign(
                startDateTime: clock.GetOffsetDateTime().Minus(NodaTime.Duration.FromDays(7)),
                endDateTime: clock.GetOffsetDateTime().Plus(NodaTime.Duration.FromDays(7)),
                editionId: 1);
            var campaignRepo = Mock.Of<ICampaignRepository>(repo => repo.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);

            var edition = new EditionDetails() { StartDateTime = clock.GetCurrentInstant().Minus(NodaTime.Duration.FromDays(90)), EndDateTime = clock.GetCurrentInstant().Minus(NodaTime.Duration.FromDays(30)) };
            var editionRepo = Mock.Of<IEditionProvider>(provider => provider.GetEdition(1) == Task.FromResult(Maybe<EditionDetails>.From(edition)), MockBehavior.Strict);

            var principalFactory = new CandidateUserClaimPrincipalFactory(userClaimPrincipalFactory, enrollmentRepo, campaignRepo, editionRepo, clock);

            // Act
            var principal = await principalFactory.CreateAsync(user);

            //Assert
            Assert.DoesNotContain(principal.Claims, claim => claim.Type == SharedKernel.ClaimTypes.Candidate);
        }
    }
}
