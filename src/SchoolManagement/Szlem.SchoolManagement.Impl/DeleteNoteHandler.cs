using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.Interfaces;
using Szlem.SharedKernel;

namespace Szlem.SchoolManagement.Impl
{
    internal class DeleteNoteHandler : IRequestHandler<DeleteNote.Command, Result<Nothing, Error>>
    {
        private readonly IUserAccessor _userAccessor;
        private readonly IAggregateStore _aggregateStore;
        private readonly IClock _clock;
        private readonly IAuthorizationService _authService;
        public DeleteNoteHandler(
            IUserAccessor userAccessor,
            IAggregateStore aggregateStore,
            IClock clock,
            IAuthorizationService authService)
        {
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<Result<Nothing, Error>> Handle(DeleteNote.Command request, CancellationToken cancellationToken)
        {
            var deletingUser = await _userAccessor.GetUser();

            var result = await _aggregateStore.Update<SchoolAggregate, SchoolId, Result<Nothing, Error>>(
                SchoolId.With(request.SchoolId), CommandId.New,
                async (aggregate, token) => {
                    var notes = aggregate.Notes.Where(x => x.NoteId == request.NoteId).ToList();
                    var note = aggregate.Notes.SingleOrDefault(x => x.NoteId == request.NoteId);
                    return
                        await _authService.AuthorizeAsResult(await _userAccessor.GetClaimsPrincipal(), note, AuthorizationPolicies.OwningCoordinatorOnly)
                        .Tap(result => aggregate.DeleteNote(request, deletingUser, _clock.GetCurrentInstant()));
                },
                cancellationToken);
            return result.Unwrap();
        }
    }
}
