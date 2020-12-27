using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.SchoolManagement.Tests
{
    public class GetDetailsTests
    {
        private readonly NodaTime.IClock _clock = new NodaTime.Testing.FakeClock(NodaTime.SystemClock.Instance.GetCurrentInstant());
        private void ConfigureServices(IServiceCollection services)
        {
            var userAccessor = Mock.Of<IUserAccessor>(ua => ua.GetUser() == Task.FromResult(new Models.Users.ApplicationUser() { Id = Guid.NewGuid() }));
            services.AddSingleton<NodaTime.IClock>(_clock);
            services.AddSingleton(userAccessor);

            var userStore = Mock.Of<IUserStore<ApplicationUser>>(
                um => um.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.FromResult(new ApplicationUser()), MockBehavior.Strict);
            services.AddSingleton(new UserManager<ApplicationUser>(userStore, null, null, null, null, null, null, null, null));
        }

        [Fact(DisplayName = "Zapytanie o nieistniejącą szkołę zwraca Error.ResourceNotFound")]
        public async Task Querying_for_nonexistent_school_returns_not_found_failure()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = Guid.Empty });

                Assert.False(result.IsSuccess);
                var error = Assert.IsType<Szlem.Domain.Error.ResourceNotFound>(result.Error);
            }
        }

        [Fact(DisplayName = "Zapytanie o istniejącą szkołę zwraca podsumowanie tej szkoły")]
        public async Task Querying_for_existing_school_returns_that_school_summary()
        {
            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                Assert.True(result.IsSuccess);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = guid });

                Assert.True(result.IsSuccess);
                var school = result.Value;
                school.Name.Should().Be("I Liceum Ogólnokształcące");
                school.City.Should().Be("Gdańsk");
                school.Address.Should().Be("Wały Piastowskie 6");
                school.ContactData.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new ContactData()
                    {
                        Name = "sekretariat",
                        EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                        PhoneNumber = PhoneNumber.Parse("58 301-67-34")
                    });
            }
        }

        [Fact(DisplayName = "Zapytanie o istniejącą szkołę zwraca eventy tej szkoły")]
        public async Task Querying_for_existing_school_returns_that_school_events()
        {
            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                Assert.True(result.IsSuccess);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = guid });

                Assert.True(result.IsSuccess);
                var school = result.Value;
                school.Name.Should().Be("I Liceum Ogólnokształcące");
                school.City.Should().Be("Gdańsk");
                school.Address.Should().Be("Wały Piastowskie 6");
                school.Events.Should().ContainSingle()
                    .Which.Should()
                    .BeEquivalentTo(new GetDetails.SchoolRegisteredEventData() { DateTime = _clock.GetOffsetDateTime() });
            }
        }


        [Fact(DisplayName = "Jeśli szkoła wyraziła wstępną zgodę, wynik zawiera flagę HasAgreedInitially")]
        public async Task If_school_agreed_initially__result_has_HasAgreedInitially()
        {
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RecordInitialAgreement.Command() {
                    SchoolId = schoolId, AgreeingPersonName = "Andrzej Strzelba"
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = schoolId });

                result.IsSuccess.Should().BeTrue();
                var school = result.Value;
                school.Should().BeEquivalentTo(new GetDetails.SchoolDetails() {
                        Id = schoolId, Name = "I Liceum Ogólnokształcące",
                        Address = "Wały Piastowskie 6", City = "Gdańsk",
                        HasAgreedInitially = true
                    },
                    options => options.Excluding(x => x.Events).Excluding(x => x.ContactData));
            }
        }

        [Fact(DisplayName = "Jeśli szkoła podpisała umowę na czas nieokreślony, wynik zawiera flagę HasSignedPermanentAgreement")]
        public async Task If_school_signed_permanent_agreement__result_has_HasSignedPermanentAgreement()
        {
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RecordAgreementSigned.Command() {
                    SchoolId = schoolId, Duration = RecordAgreementSigned.AgreementDuration.Permanent,
                    ScannedDocument = new byte[] { 0x00 }, ScannedDocumentContentType = "image/jpeg", ScannedDocumentExtension = ".jpg"
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = schoolId });

                result.IsSuccess.Should().BeTrue();
                var school = result.Value;
                school.Should().BeEquivalentTo(new GetDetails.SchoolDetails() {
                        Id = schoolId, Name = "I Liceum Ogólnokształcące",
                        Address = "Wały Piastowskie 6", City = "Gdańsk",
                        HasSignedPermanentAgreement = true
                    },
                    options => options.Excluding(x => x.Events).Excluding(x => x.ContactData));
            }
        }

        [Fact(DisplayName = "Jeśli szkoła podpisała umowę na czas określony, wynik zawiera flagę HasSignedFixedTermAgreement i wartość AgreementEndDate")]
        public async Task If_school_signed_fixed_term_agreement__result_has_HasSignedFixedTermAgreement_and_AgreementEndDate()
        {
            Guid schoolId;
            var now = NodaTime.SystemClock.Instance.GetZonedDateTime();
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RecordAgreementSigned.Command() {
                    SchoolId = schoolId,
                    Duration = RecordAgreementSigned.AgreementDuration.FixedTerm, AgreementEndDate = now.Plus(NodaTime.Duration.FromDays(7)).Date,
                    ScannedDocument = new byte[] { 0x00 }, ScannedDocumentContentType = "image/jpeg", ScannedDocumentExtension = ".jpg"
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = schoolId });

                result.IsSuccess.Should().BeTrue();
                var school = result.Value;
                school.Should().BeEquivalentTo(new GetDetails.SchoolDetails() {
                        Id = schoolId, Name = "I Liceum Ogólnokształcące",
                        Address = "Wały Piastowskie 6", City = "Gdańsk",
                        HasSignedFixedTermAgreement = true, AgreementEndDate = now.Plus(NodaTime.Duration.FromDays(7)).Date
                    },
                    options => options.Excluding(x => x.Events).Excluding(x => x.ContactData));
            }
        }

        [Fact(DisplayName = "Jeśli szkoła zrezygnowała bezterminowo, wynik zawiera flagę HasResignedPermanently")]
        public async Task If_school_resigned_permanently__result_has_flag_HasResignedPermanently()
        {
            Guid schoolId;
            var now = NodaTime.SystemClock.Instance.GetZonedDateTime();
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RecordResignation.Command() {
                    SchoolId = schoolId
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = schoolId });

                result.IsSuccess.Should().BeTrue();
                var school = result.Value;
                school.Should().BeEquivalentTo(new GetDetails.SchoolDetails() {
                        Id = schoolId, Name = "I Liceum Ogólnokształcące",
                        Address = "Wały Piastowskie 6", City = "Gdańsk",
                        HasResignedPermanently = true
                    },
                    options => options.Excluding(x => x.Events).Excluding(x => x.ContactData));
            }
        }

        [Fact(DisplayName = "Jeśli szkoła zrezygnowała terminowo, wynik zawiera flagę HasResignedTemporarily i wartość ResignationEndDate")]
        public async Task If_school_resigned_temporarily__result_has_flag_HasResignedTemporarily_and_ResignationEndDate()
        {
            Guid schoolId;
            var now = NodaTime.SystemClock.Instance.GetZonedDateTime();
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new RecordResignation.Command() {
                    SchoolId = schoolId, PotentialNextContactDate = now.Plus(NodaTime.Duration.FromDays(7)).Date
                };
                var result = await engine.Execute(command);
                result.IsSuccess.Should().BeTrue();
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetDetails.Query() { SchoolId = schoolId });

                result.IsSuccess.Should().BeTrue();
                var school = result.Value;
                school.Should().BeEquivalentTo(new GetDetails.SchoolDetails() {
                        Id = schoolId, Name = "I Liceum Ogólnokształcące",
                        Address = "Wały Piastowskie 6", City = "Gdańsk",
                        HasResignedTemporarily = true,
                        ResignationEndDate = now.Plus(NodaTime.Duration.FromDays(7)).Date
                    },
                    options => options.Excluding(x => x.Events).Excluding(x => x.ContactData));
            }
        }
    }
}
