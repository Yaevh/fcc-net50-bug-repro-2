using CSharpFunctionalExtensions;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Impl.Trainings
{
    internal class AddNoteHandler : IRequestHandler<AddNote.Command, Result<Nothing, Error>>
    {
        private readonly ITrainingRepository _trainingRepo;
        private readonly IClock _clock;
        private readonly IUserAccessor _userAccessor;

        public AddNoteHandler(ITrainingRepository trainingRepo, IClock clock, IUserAccessor userAccessor)
        {
            _trainingRepo = trainingRepo ?? throw new ArgumentNullException(nameof(trainingRepo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        }

        public async Task<Result<Nothing, Error>> Handle(AddNote.Command request, CancellationToken cancellationToken)
        {
            var maybeTraining = await _trainingRepo.GetById(request.TrainingId);
            if (maybeTraining.HasNoValue)
                return Result.Failure<Nothing, Error>(new Error.ResourceNotFound(ErrorMessages.TrainingNotFound));

            var training = maybeTraining.Value;
            var user = await _userAccessor.GetUser();
            var now = _clock.GetCurrentInstant();

            var result = await training.AddNote(user.Id, request.Content, now)
                .Tap(async () => await _trainingRepo.Update(training));
            
            return result
                .Match(
                    onSuccess: () => Result.Success<Nothing, Error>(Nothing.Value),
                    error => Result.Failure<Nothing, Error>(new Error.BadRequest(error))
                );
        }
    }
}
