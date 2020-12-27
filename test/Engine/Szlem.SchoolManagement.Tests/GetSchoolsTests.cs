using EventFlow.Aggregates;
using FluentAssertions;
using MediatR;
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
using Szlem.SchoolManagement.Impl;
using Szlem.SharedKernel;
using X.PagedList;
using Xunit;

namespace Szlem.SchoolManagement.Tests
{
    public class GetSchoolsTests
    {
        [Fact(DisplayName = "Handler potrafi wyszukać szkołę po nazwie")]
        public async Task Handler_can_search_schools_by_name()
        {
            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var userAccessor = Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict);
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var handler = new RegisterSchoolHandler(NodaTime.SystemClock.Instance, userAccessor, aggregateStore);

                var command1 = new RegisterSchool.Command() {
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
                var command2 = new RegisterSchool.Command() {
                    Name = "XV Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Pilotów 7",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("sekretariat@lo15.edu.gdansk.pl"),
                            PhoneNumber = PhoneNumber.Parse("(58) 556-51-95") },
                    }
                };
                var result = await handler.Handle(command1, CancellationToken.None);
                var result2 = await handler.Handle(command2, CancellationToken.None);

                Assert.True(result.IsSuccess);
                Assert.True(result2.IsSuccess);
                Assert.IsType<Guid>(result.Value);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<GetSchools.Query, IPagedList<GetSchools.Summary>>>();
                var results = await handler.Handle(new GetSchools.Query() { SearchPattern = "I liceum" }, CancellationToken.None);

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = guid,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                    });
            }
        }

        [Fact(DisplayName = "Handler potrafi wyszukać szkołę po mieście")]
        public async Task Handler_can_search_school_by_city()
        {
            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var userAccessor = Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict);
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var handler = new RegisterSchoolHandler(NodaTime.SystemClock.Instance, userAccessor, aggregateStore);

                var command1 = new RegisterSchool.Command() {
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
                var command2 = new RegisterSchool.Command()
                {
                    Name = "III Liceum Ogólnokształcące",
                    City = "Gdynia",
                    Address = "Legionów 27",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("sekretariat@lo3.gdynia.pl"),
                            PhoneNumber = PhoneNumber.Parse("(58) 622 18 33") },
                    }
                };
                var result = await handler.Handle(command1, CancellationToken.None);
                var result2 = await handler.Handle(command2, CancellationToken.None);

                Assert.True(result.IsSuccess);
                Assert.True(result2.IsSuccess);
                Assert.IsType<Guid>(result.Value);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<GetSchools.Query, IPagedList<GetSchools.Summary>>>();
                var results = await handler.Handle(new GetSchools.Query() { SearchPattern = "gdańsk" }, CancellationToken.None);

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = guid,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                    });
            }
        }

        [Fact(DisplayName = "Handler potrafi wyszukać szkołę po ulicy")]
        public async Task Handler_can_search_school_by_street()
        {
            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var userAccessor = Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict);
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var handler = new RegisterSchoolHandler(NodaTime.SystemClock.Instance, userAccessor, aggregateStore);

                var command1 = new RegisterSchool.Command() {
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
                var command2 = new RegisterSchool.Command() {
                    Name = "XV Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Pilotów 7",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("sekretariat@lo15.edu.gdansk.pl"),
                            PhoneNumber = PhoneNumber.Parse("(58) 556-51-95") },
                    }
                };
                var result = await handler.Handle(command1, CancellationToken.None);
                var result2 = await handler.Handle(command2, CancellationToken.None);

                Assert.True(result.IsSuccess);
                Assert.True(result2.IsSuccess);
                Assert.IsType<Guid>(result.Value);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<GetSchools.Query, IPagedList<GetSchools.Summary>>>();
                var results = await handler.Handle(new GetSchools.Query() { SearchPattern = "wały" }, CancellationToken.None);

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = guid,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła jest nowa, podsumowanie ma status Unknown")]
        public async Task If_school_is_new__summary_status_is_Unknown()
        {
            // arrange
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            // act & assert
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.Unknown
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła wyraziła wstępną zgodę, podsumowanie ma status HasAgreedInitially")]
        public async Task If_school_has_agreed_initially__summary_status_is_HasAgreedInitially()
        {
            // arrange
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RecordInitialAgreement.Command() {
                    SchoolId = schoolId, AgreeingPersonName = "Andrzej Strzelba"
                });
                Assert.True(result.IsSuccess);
            }

            // act & assert
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.HasAgreedInitially
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła podpisała umowę, podsumowanie ma status HasSignedAgreement")]
        public async Task If_school_has_signed_the_agreement__summary_status_is_HasSignedAgreement()
        {
            // arrange
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RecordAgreementSigned.Command() {
                    SchoolId = schoolId,
                    ScannedDocument = new byte[] { 0x00 },
                    ScannedDocumentContentType = "image/jpeg", ScannedDocumentExtension = ".jpg",
                    Duration = RecordAgreementSigned.AgreementDuration.Permanent
                });
                Assert.True(result.IsSuccess);
            }

            // act & assert
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.HasSignedAgreement
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła podpisała umowę na czas określony, a czas umowy zakończył się, podsumowanie ma status Unknown")]
        public async Task If_school_has_signed_agreement_but_agreement_time_expired__summary_status_is_Unknown()
        {
            // arrange
            Guid schoolId;
            var clock = new NodaTime.Testing.FakeClock(NodaTime.SystemClock.Instance.GetCurrentInstant());
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
                services.AddSingleton<NodaTime.IClock>(clock);
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RecordAgreementSigned.Command() {
                    SchoolId = schoolId,
                    ScannedDocument = new byte[] { 0x00 },
                    ScannedDocumentContentType = "image/jpeg", ScannedDocumentExtension = ".jpg",
                    Duration = RecordAgreementSigned.AgreementDuration.FixedTerm,
                    AgreementEndDate = clock.GetTodayDate() + NodaTime.Period.FromDays(1)
                });
                Assert.True(result.IsSuccess);
            }

            // act & assert
            clock.AdvanceDays(3);

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.Unknown
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła zrezygnowała, podsumowanie ma status HasResigned")]
        public async Task If_school_has_resigned__summary_status_is_HasResigned()
        {
            // arrange
            Guid schoolId;
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RecordResignation.Command() { SchoolId = schoolId });
                Assert.True(result.IsSuccess);
            }

            // act & assert
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.HasResigned
                    });
            }
        }

        [Fact(DisplayName = "Jeśli szkoła zrezygnowała, a okres rezygnacji zakończył się, podsumowanie ma status Unknown")]
        public async Task If_school_has_resigned_but_resignation_time_expired__summary_status_is_Unknown()
        {
            // arrange
            Guid schoolId;
            var clock = new NodaTime.Testing.FakeClock(NodaTime.SystemClock.Instance.GetCurrentInstant());
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                services.AddSingleton(Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict));
                services.AddSingleton<NodaTime.IClock>(clock);
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                });
                schoolId = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Execute(new RecordResignation.Command() {
                    SchoolId = schoolId,
                    PotentialNextContactDate = clock.GetTodayDate() + NodaTime.Period.FromDays(1) });
                Assert.True(result.IsSuccess);
            }

            // act & assert
            clock.AdvanceDays(3);

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var results = await engine.Query(new GetSchools.Query());

                results.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo(new GetSchools.Summary() {
                        Id = schoolId,
                        Name = "I Liceum Ogólnokształcące",
                        City = "Gdańsk",
                        Address = "Wały Piastowskie 6",
                        Status = GetSchools.SchoolStatus.Unknown
                    });
            }
        }
    }
}
