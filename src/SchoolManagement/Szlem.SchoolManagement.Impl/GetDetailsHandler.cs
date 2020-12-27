using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.SchoolManagement.Impl.Events;

namespace Szlem.SchoolManagement.Impl
{
    internal class GetDetailsHandler : IRequestHandler<GetDetails.Query, Result<GetDetails.SchoolDetails, Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IEventStore _eventStore;
        private readonly IClock _clock;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.Users.ApplicationUser> _userManager;

        public GetDetailsHandler(
            IAggregateStore aggregateStore,
            IEventStore eventStore,
            IClock clock,
            Microsoft.AspNetCore.Identity.UserManager<Models.Users.ApplicationUser> userManager)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }


        public async Task<Result<GetDetails.SchoolDetails, Error>> Handle(GetDetails.Query request, CancellationToken cancellationToken)
        {
            var id = SchoolId.With(request.SchoolId);

            var events = await _eventStore.LoadEventsAsync<SchoolAggregate, SchoolId>(id, cancellationToken);
            var school = await _aggregateStore.LoadAsync<SchoolAggregate, SchoolId>(id, cancellationToken);

            if (school.IsNew)
                return Result.Failure<GetDetails.SchoolDetails, Error>(new Error.ResourceNotFound());

            var details = await BuildDetails(school, events);

            return Result.Success<GetDetails.SchoolDetails, Error>(details);
        }

        private async Task<GetDetails.SchoolDetails> BuildDetails(SchoolAggregate school, IReadOnlyCollection<IDomainEvent<SchoolAggregate, SchoolId>> events)
        {
            var today = _clock.GetTodayDate();
            return new GetDetails.SchoolDetails() {
                Id = school.Id.GetGuid(),
                Name = school.SchoolName,
                Address = school.Address,
                City = school.City,
                ContactData = school.ContactData,
                Events = await BuildEventData(school, events),
                Notes = await Task.WhenAll(school.Notes.Select(async x => new GetDetails.NoteData() {
                        Id = x.NoteId,
                        CreatedAt = x.CreationTimestamp.InMainTimezone().ToOffsetDateTime(),
                        EditedAt = x.LastEditTimestamp?.InMainTimezone().ToOffsetDateTime(), WasEdited = x.WasEdited,
                        Author = await GetUserName(x.AuthorId), Content = x.Content })),
                HasAgreedInitially = school.HasAgreedInitially,
                HasSignedPermanentAgreement = school.HasSignedPermanentAgreement,
                HasSignedFixedTermAgreement = school.HasSignedFixedTermAgreement(today),
                AgreementEndDate = school.AgreementEndDate,
                HasResignedPermanently = school.HasResignedPermanently,
                HasResignedTemporarily = school.HasResignedTemporarily(today),
                ResignationEndDate = school.ResignationEndDate
            };
        }

        private async Task<IReadOnlyCollection<GetDetails.EventData>> BuildEventData(SchoolAggregate school, IEnumerable<IDomainEvent<SchoolAggregate, SchoolId>> events)
        {
            var eventData = await Task.WhenAll(events.Select(async @event => await BuildEventData(school, @event)));
            return eventData.Where(x => x != null).ToList();
        }

        private async Task<GetDetails.EventData> BuildEventData(SchoolAggregate school, IDomainEvent<SchoolAggregate, SchoolId> @event)
        {
            switch (@event.GetAggregateEvent())
            {
                case SchoolRegistered e:
                    return new GetDetails.SchoolRegisteredEventData() { DateTime = e.Timestamp.InMainTimezone().ToOffsetDateTime() };
                case ContactOccured e:
                    return new GetDetails.ContactOccuredEventData() {
                        DateTime = e.ContactTimestamp.InMainTimezone().ToOffsetDateTime(),
                        CommunicationChannel = e.CommunicationChannel, ContactPersonName = e.ContactPersonName,
                        PhoneNumber = e.PhoneNumber, EmailAddress = e.EmailAddress,
                        Content = e.Content, AdditionalNotes = e.AdditionalNotes, RecordingUser = await GetUserName(e.RecordingUserId)
                    };
                case InitialAgreementAchieved e:
                    return new GetDetails.InitialAgreementAchievedEventData() {
                        DateTime = OffsetDateTime.FromDateTimeOffset(@event.Timestamp),
                        AgreeingPersonName = e.AgreeingPersonName,
                        AdditionalNotes = e.AdditionalNotes, RecordingUser = await GetUserName(e.RecordingUserId)
                    };
                case FixedTermAgreementSigned e:
                    return new GetDetails.FixedTermAgreementSignedEventData() {
                        AgreementId = e.Id,
                        DateTime = OffsetDateTime.FromDateTimeOffset(@event.Timestamp),
                        AgreementEndDate = e.AgreementEndDate,
                        AdditionalNotes = e.AdditionalNotes, RecordingUser = await GetUserName(e.RecordingUserId)
                    };
                case PermanentAgreementSigned e:
                    return new GetDetails.PermanentAgreementSignedEventData() {
                        AgreementId = e.Id,
                        DateTime = OffsetDateTime.FromDateTimeOffset(@event.Timestamp),
                        AdditionalNotes = e.AdditionalNotes, RecordingUser = await GetUserName(e.RecordingUserId)
                    };
                case SchoolResignedFromCooperation e:
                    return new GetDetails.SchoolResignedFromCooperationEventData() {
                        DateTime = OffsetDateTime.FromDateTimeOffset(@event.Timestamp),
                        PotentialNextContactDate = e.PotentialNextContactDate,
                        AdditionalNotes = e.AdditionalNotes, RecordingUser = await GetUserName(e.RecordingUserId)
                    };
                case NoteAdded _:
                case NoteEdited _:
                case NoteDeleted _:
                    return null;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task<string> GetUserName(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user.ToString();
        }
    }
}
