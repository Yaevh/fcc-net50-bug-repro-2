using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.SchoolManagement.Impl.Events;

namespace Szlem.SchoolManagement.Impl
{
    internal class GetScannedAgreementHandler : IRequestHandler<GetScannedAgreement.Query, Maybe<GetScannedAgreement.ScannedAgreement>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IEventStore _eventStore;
        public GetScannedAgreementHandler(IAggregateStore aggregateStore, IEventStore eventStore)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task<Maybe<GetScannedAgreement.ScannedAgreement>> Handle(GetScannedAgreement.Query request, CancellationToken cancellationToken)
        {
            var events = await _eventStore.LoadEventsAsync<SchoolAggregate, SchoolId>(SchoolId.With(request.SchoolId), cancellationToken);
            if (events.None())
                return Maybe<GetScannedAgreement.ScannedAgreement>.None;

            var permanentAgreement = events
                .Where(x => x.GetAggregateEvent() is PermanentAgreementSigned)
                .Cast<IDomainEvent<SchoolAggregate, SchoolId, PermanentAgreementSigned>>()
                .Select(x => x.AggregateEvent)
                .SingleOrDefault(x => x.Id == request.AgreementId);
            if (permanentAgreement != null)
                return Maybe<GetScannedAgreement.ScannedAgreement>.From(new GetScannedAgreement.ScannedAgreement() {
                    Content = permanentAgreement.ScannedDocument,
                    ContentType = permanentAgreement.ScannedDocumentContentType,
                    FileName = $"{request.SchoolId}{permanentAgreement.ScannedDocumentExtension}"
                });

            var fixedTermAgreement = events
                .Where(x => x.GetAggregateEvent() is FixedTermAgreementSigned)
                .Cast<IDomainEvent<SchoolAggregate, SchoolId, FixedTermAgreementSigned>>()
                .Select(x => x.AggregateEvent)
                .SingleOrDefault(x => x.Id == request.AgreementId);
            if (fixedTermAgreement != null)
                return Maybe<GetScannedAgreement.ScannedAgreement>.From(new GetScannedAgreement.ScannedAgreement() {
                    Content = fixedTermAgreement.ScannedDocument,
                    ContentType = fixedTermAgreement.ScannedDocumentContentType,
                    FileName = $"{request.SchoolId}{fixedTermAgreement.ScannedDocumentExtension}"
                });

            return Maybe<GetScannedAgreement.ScannedAgreement>.None;
        }
    }
}
