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
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using static Szlem.Recruitment.Impl.Enrollments.EnrollmentAggregate;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class RecordTrainingResultsHandler : IRequestHandler<RecordTrainingResults.Command, Result<Nothing, Domain.Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IClock _clock;
        private readonly ITrainingRepository _trainingRepository;
        private readonly IUserAccessor _userAccessor;

        public RecordTrainingResultsHandler(IAggregateStore aggregateStore, IClock clock, ITrainingRepository trainingRepository, IUserAccessor userAccessor)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trainingRepository = trainingRepository ?? throw new ArgumentNullException(nameof(trainingRepository));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordTrainingResults.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();

            var selectedTraining = await _trainingRepository.GetById(request.TrainingId);
            if (selectedTraining.HasNoValue)
                return Result.Failure<Nothing, Error>(new Error.ResourceNotFound($"Training with ID={request.TrainingId} not found"));

            var enrollment = await _aggregateStore.LoadAsync<EnrollmentAggregate, EnrollmentId>(EnrollmentId.With(request.EnrollmentId), cancellationToken);
            var preferredTrainings = await _trainingRepository.GetByIds(enrollment.PreferredTrainingIds);

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                EnrollmentId.With(request.EnrollmentId), CommandId.New,
                (aggregate) => aggregate.RecordTrainingResults(request, user, preferredTrainings, selectedTraining.Value, _clock.GetCurrentInstant()),
                cancellationToken);

            return result.Unwrap();
        }
    }
}
