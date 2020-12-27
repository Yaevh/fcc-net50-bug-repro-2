using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using MediatR;
using Microsoft.Extensions.Options;
using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class SendTrainingReminderHandler : IRequestHandler<SendTrainingReminder.Command, Result<Nothing, Error>>
    {
        private readonly IClock _clock;
        private readonly IAggregateStore _aggregateStore;
        private readonly ITrainingRepository _repo;
        private readonly IEmailService _emailService;
        private readonly IOptions<Config> _options;
        private readonly IFluidTemplateRenderer _fluidTemplateRenderer;
        public SendTrainingReminderHandler(
            IClock clock,
            IAggregateStore aggregateStore,
            ITrainingRepository repo,
            IEmailService emailService,
            IOptions<Config> options,
            IFluidTemplateRenderer fluidTemplateRenderer)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _fluidTemplateRenderer = fluidTemplateRenderer ?? throw new ArgumentNullException(nameof(fluidTemplateRenderer));
        }


        public async Task<Result<Nothing, Error>> Handle(SendTrainingReminder.Command command, CancellationToken cancellationToken)
        {
            var training = await _repo.GetById(command.TrainingId);
            if (training.HasNoValue)
                return Result.Failure<Nothing, Error>(new Error.ResourceNotFound(SendTrainingReminder_Messages.Training_not_found));

            var options = _options.Value.TrainingReminderEmail;
            var enrollmentId = EnrollmentAggregate.EnrollmentId.With(command.EnrollmentId);

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, Result<Nothing, Error>>(
                enrollmentId, EventFlow.Core.SourceId.New,
                async (aggregate, token) => {
                    return await aggregate.CanSendTrainingReminder(command, training.Value, _clock.GetCurrentInstant())
                        .Tap(async _ => {
                            var body = await BuildMessageBody(options, aggregate, training.Value);
                            var message = _emailService.CreateMessage(aggregate.Email, options.Subject, body, isBodyHtml: options.IsBodyHtml);
                            await _emailService.Send(message, token)
                                .Tap(() => aggregate.RecordEmailSent(_clock.GetCurrentInstant(), message))
                                .OnFailure(() => aggregate.RecordEmailSendingFailed(_clock.GetCurrentInstant(), message));
                        });
                },
                cancellationToken);
            return result.Unwrap();
        }


        private async Task<string> BuildMessageBody(Config.EmailMessageConfig emailConfig, EnrollmentAggregate aggregate, Entities.Training training)
        {
            var body = await emailConfig.BuildMessageBody();
            var model = new {
                Candidate = new { aggregate.FirstName, aggregate.LastName, aggregate.FullName, aggregate.Email, aggregate.PhoneNumber, aggregate.Region },
                Training = new {
                    training.Address, training.City, training.StartDateTime, training.EndDateTime,
                    StartDate = training.StartDateTime.Date, StartTime = training.StartDateTime.TimeOfDay,
                    EndDate = training.EndDateTime.Date, EndTime = training.EndDateTime.TimeOfDay,
                    Duration = training.Duration.ToString("HH':'mm", null)
                }
            };
            return _fluidTemplateRenderer.Render(body, model);
        }
    }
}
