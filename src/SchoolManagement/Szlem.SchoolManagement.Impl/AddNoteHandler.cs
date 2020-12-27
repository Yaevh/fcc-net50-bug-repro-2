using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;

namespace Szlem.SchoolManagement.Impl
{
    internal class AddNoteHandler : IRequestHandler<AddNote.Command, Result<Guid, Error>>
    {
        private readonly IUserAccessor _userAccessor;
        private readonly IAggregateStore _aggregateStore;
        private readonly IClock _clock;
        public AddNoteHandler(IUserAccessor userAccessor, IAggregateStore aggregateStore, IClock clock)
        {
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<Result<Guid, Error>> Handle(AddNote.Command request, CancellationToken cancellationToken)
        {
            var author = await _userAccessor.GetUser();

            var result = await _aggregateStore.Update<SchoolAggregate, SchoolId, Result<Guid, Error>>(
                SchoolId.With(request.SchoolId), CommandId.New,
                (aggregate) => aggregate.AddNote(request, author, _clock.GetCurrentInstant()),
                cancellationToken);
            return result.Unwrap();
        }
    }
}
