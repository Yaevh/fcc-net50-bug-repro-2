using EventFlow.Aggregates;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;
using Szlem.SchoolManagement.Impl;
using Szlem.SchoolManagement.Impl.Events;
using Xunit;

namespace Szlem.SchoolManagement.Tests
{
    public class RegisterSchoolTests
    {
        [Fact(DisplayName = "Rejestracja szkoły musi zawierać nazwę, miasto, adres i co najmniej jeden kontakt")]
        public void Submission_must_contain__name_city_address_and_at_least_one_contact_with_email_or_phone_number()
        {
            var command = new RegisterSchool.Command() {
                Name = string.Empty, City = string.Empty, Address = string.Empty,
                ContactData = Array.Empty<ContactData>()
            };
            var school = new SchoolAggregate(SchoolId.New);
            var now = NodaTime.SystemClock.Instance.GetCurrentInstant();

            var result = school.RegisterSchool(now, command, new ApplicationUser());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Contains(RegisterSchool_Messages.SchoolName_CannotBeEmpty, error.Failures[nameof(command.Name)]);
            Assert.Contains(RegisterSchool_Messages.City_CannotBeEmpty, error.Failures[nameof(command.City)]);
            Assert.Contains(RegisterSchool_Messages.Address_Cannot_be_empty, error.Failures[nameof(command.Address)]);
            Assert.Contains(RegisterSchool_Messages.ContactData_cannot_be_empty, error.Failures[nameof(command.ContactData)]);
        }
        
        [Fact(DisplayName = "Każdy kontakt w rejestracji szkoły musi zawierać email lub telefon")]
        public void Each_contact_must_contain_email_or_phone_number()
        {
            var command = new RegisterSchool.Command() {
                Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk",
                Address = "Wały Piastowskie 6",
                ContactData = new[] {
                    new ContactData() { Name = "sekretariat" }
                }
            };
            var school = new SchoolAggregate(SchoolId.New);
            var now = NodaTime.SystemClock.Instance.GetCurrentInstant();

            var result = school.RegisterSchool(now, command, new ApplicationUser());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Collection(error.Failures,
                first => {
                    Assert.Equal($"{nameof(command.ContactData)}[0].{nameof(ContactData.PhoneNumber)}", first.PropertyName);
                    Assert.Single(first.Errors, RegisterSchool_Messages.Either_ContactData_PhoneNumber_or_ContactData_EmailAddress_must_be_provided);
                },
                second =>
                {
                    Assert.Equal($"{nameof(command.ContactData)}[0].{nameof(ContactData.EmailAddress)}", second.PropertyName);
                    Assert.Single(second.Errors, RegisterSchool_Messages.Either_ContactData_PhoneNumber_or_ContactData_EmailAddress_must_be_provided);
                });
        }

        [Fact(DisplayName = "Emaile i numery telefonów nie mogą się powtarzać w danych kontaktowych")]
        public void Emails_and_phone_numbers_cannot_be_duplicates()
        {
            var command = new RegisterSchool.Command() {
                Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk",
                Address = "Wały Piastowskie 6",
                ContactData = new[] {
                    new ContactData() { Name = "sekretariat", EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"), PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    new ContactData() { Name = "dyrektor", EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"), PhoneNumber = PhoneNumber.Parse("58 301-67-34") }
                }
            };
            var school = new SchoolAggregate(SchoolId.New);
            var now = NodaTime.SystemClock.Instance.GetCurrentInstant();

            var result = school.RegisterSchool(now, command, new ApplicationUser());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(command.ContactData), failure.PropertyName);
            Assert.All(failure.Errors, message => Assert.Equal(RegisterSchool_Messages.ContactData_emails_and_phone_numbers_cannot_repeat_themselves, message));
        }

        [Fact(DisplayName = "Po rejestracji szkoły, handler zwraca Id szkoły")]
        public async Task After_submission__handler_returns_school_Id()
        {
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

            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var userAccessor = Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict);
                var handler = new RegisterSchoolHandler(NodaTime.SystemClock.Instance, userAccessor, aggregateStore);
                var result = await handler.Handle(command, CancellationToken.None);

                Assert.True(result.IsSuccess);
                Assert.IsType<Guid>(result.Value);
            }
        }

        [Fact(DisplayName = "Po rejestracji szkoły, baza zawiera agregat szkoły")]
        public async Task After_submission__database_contains_school_aggregate()
        {
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

            Guid guid;
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var userAccessor = Mock.Of<IUserAccessor>(
                    mock => mock.GetUser() == Task.FromResult(new ApplicationUser() { Id = Guid.NewGuid() }),
                    MockBehavior.Strict);
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var handler = new RegisterSchoolHandler(NodaTime.SystemClock.Instance, userAccessor, aggregateStore);
                var result = await handler.Handle(command, CancellationToken.None);

                Assert.True(result.IsSuccess);
                Assert.IsType<Guid>(result.Value);
                guid = result.Value;
            }

            using (var scope = sp.CreateScope())
            {
                var aggregateStore = scope.ServiceProvider.GetRequiredService<IAggregateStore>();
                var school = await aggregateStore.LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(guid), CancellationToken.None);
                Assert.NotNull(school);
                Assert.False(school.IsNew);
            }
        }

        [Fact(DisplayName = "Po rejestracji szkoły, agregat zawiera event SchoolSubmitted")]
        public void After_submission__aggregate_contains_SchoolRegistered_event()
        {
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

            var school = new SchoolAggregate(SchoolId.New);
            var registeringUser = new ApplicationUser() { Id = Guid.NewGuid() };
            var result = school.RegisterSchool(NodaTime.SystemClock.Instance.GetCurrentInstant(), command, registeringUser);

            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(school.UncommittedEvents, e => e.AggregateEvent is SchoolRegistered);
            var @event = Assert.IsType<SchoolRegistered>(uncommittedEvent.AggregateEvent);
            Assert.Equal(registeringUser.Id, @event.RegisteringUserId);
            Assert.Equal(command.Name, @event.Name);
            Assert.Equal(command.City, @event.City);
            var contactData = Assert.Single(command.ContactData);
            Assert.Equal("sekretariat", contactData.Name);
            Assert.Equal(EmailAddress.Parse("szkola@ilo.gda.pl"), contactData.EmailAddress);
            Assert.Equal(PhoneNumber.Parse("58 301-67-34"), contactData.PhoneNumber);
        }
    }
}
