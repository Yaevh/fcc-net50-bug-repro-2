using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.Models.Users;
using Szlem.Persistence.EF.Services;
using Szlem.Recruitment.DependentServices;
using Xunit;

namespace Szlem.Persistence.EF.Tests
{
    public class TrainerProviderTests
    {
        private UserManager<ApplicationUser> BuildUserManagerMock(IReadOnlyCollection<Guid> guids)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            userManagerMock.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            userManagerMock.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            foreach (var guid in guids)
                userManagerMock
                    .Setup(x => x.FindByIdAsync(guid.ToString()))
                    .Returns(Task.FromResult(new ApplicationUser() { Id = guid, UserName = guid.ToString() }));

            return userManagerMock.Object;
        }

        [Fact]
        public async Task When_queried_for_single_existing_guid_provider_returns_trainer()
        {
            var guid = Guid.NewGuid();
            var trainerProvider = new TrainerProvider(BuildUserManagerMock(new[] { guid }));

            var trainer = await trainerProvider.GetTrainerDetails(guid);

            Assert.True(trainer.HasValue);
            Assert.Equal(guid, trainer.Value.Guid);
            Assert.Equal(guid.ToString(), trainer.Value.Name);
        }

        [Fact]
        public async Task When_queried_for_single_nonexisting_guid_provider_returns_none()
        {
            var guid = Guid.NewGuid();
            var trainerProvider = new TrainerProvider(BuildUserManagerMock(new[] { guid }));

            var trainer = await trainerProvider.GetTrainerDetails(Guid.NewGuid());

            Assert.False(trainer.HasValue);
        }

        [Fact]
        public async Task When_queried_for_multiple_unique_existing_guids_provider_returns_trainer_for_each_of_them()
        {
            var guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var trainerProvider = new TrainerProvider(BuildUserManagerMock(guids));

            var trainers = await trainerProvider.GetTrainerDetails(guids);

            Assert.Collection(trainers,
                first => {
                    Assert.Equal(guids[0], first.Guid);
                    Assert.Equal(guids[0].ToString(), first.Name);
                },
                second => {
                    Assert.Equal(guids[1], second.Guid);
                    Assert.Equal(guids[1].ToString(), second.Name);
                },
                third => {
                    Assert.Equal(guids[2], third.Guid);
                    Assert.Equal(guids[2].ToString(), third.Name);
                });
        }

        [Fact]
        public async Task When_queried_for_duplicate_existing_guids_provider_returns_only_single_trainer()
        {
            var guid = Guid.NewGuid();
            var trainerProvider = new TrainerProvider(BuildUserManagerMock(new[] { guid }));

            var trainers = await trainerProvider.GetTrainerDetails(new[] { guid, guid, guid });

            var trainer = Assert.Single(trainers);
            Assert.Equal(guid, trainer.Guid);
            Assert.Equal(guid.ToString(), trainer.Name);
        }

        [Fact]
        public async Task When_queried_for_multiple_nonexisting_guids_provider_returns_empty_collection()
        {
            var guid = Guid.NewGuid();
            var trainerProvider = new TrainerProvider(BuildUserManagerMock(new[] { Guid.NewGuid() }));

            var trainers = await trainerProvider.GetTrainerDetails(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

            Assert.Empty(trainers);
        }
    }
}
