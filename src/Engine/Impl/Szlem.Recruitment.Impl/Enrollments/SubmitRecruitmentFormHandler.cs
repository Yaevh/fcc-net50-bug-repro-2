using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Core;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using static Szlem.Recruitment.Impl.Enrollments.EnrollmentAggregate;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class SubmitRecruitmentFormHandler : IRequestHandler<SubmitRecruitmentForm.Command, Result<Nothing, Error>>
    {
        public const string MessageTitle = "Dziękujemy za zgłoszenie do projektu LEM";

        private readonly ITrainingRepository _repo;
        private readonly IClock _clock;
        private readonly IAggregateStore _aggregateStore;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IEmailService _emailService;
        private readonly IOptions<Config> _options;
        private readonly IFluidTemplateRenderer _fluidTemplateRenderer;
        public SubmitRecruitmentFormHandler(
            ITrainingRepository repo,
            IClock clock,
            IAggregateStore aggregateStore,
            IEnrollmentRepository enrollmentRepo,
            IEmailService emailService,
            IOptions<Config> options,
            IFluidTemplateRenderer fluidTemplateRenderer)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _fluidTemplateRenderer = fluidTemplateRenderer ?? throw new ArgumentNullException(nameof(fluidTemplateRenderer));
        }

        public async Task<Result<Nothing, Error>> Handle(SubmitRecruitmentForm.Command request, CancellationToken cancellationToken)
        {
            var options = _options.Value;
            var preferredTrainings = await _repo.GetByIds(request.PreferredTrainingIds);

            var enrollment = _enrollmentRepo.Query().Where(x => x.Email == request.Email).FirstOrDefault();
            var enrollmentId = enrollment != null ? enrollment.Id : EnrollmentId.New;

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                enrollmentId, SourceId.New,
                (aggregate) => aggregate.SubmitRecruitmentForm(request, preferredTrainings, _clock.GetCurrentInstant()),
                cancellationToken);

            return await result.Unwrap()
                .Tap(async _ =>
                {
                    await _aggregateStore.UpdateAsync<EnrollmentAggregate, EnrollmentId>(
                        enrollmentId, SourceId.New,
                        async (aggregate, token) =>
                        {
                            var body = await BuildMessageBody(options.GreetingEmail, aggregate);
                            var message = _emailService.CreateMessage(request.Email, options.GreetingEmail.Subject, body, isBodyHtml: options.GreetingEmail.IsBodyHtml);
                            await _emailService.Send(message, cancellationToken)
                                .Tap(() => aggregate.RecordEmailSent(_clock.GetCurrentInstant(), message))
                                .OnFailure(() => aggregate.RecordEmailSendingFailed(_clock.GetCurrentInstant(), message));
                        }, cancellationToken);
                });
        }

        private async Task<string> BuildMessageBody(Config.EmailMessageConfig greetingEmailConfig, EnrollmentAggregate aggregate)
        {
            var body = await greetingEmailConfig.BuildMessageBody();
            var model = new { aggregate.FirstName, aggregate.LastName, aggregate.FullName, aggregate.Email, aggregate.PhoneNumber, aggregate.Region };
            return _fluidTemplateRenderer.Render(body, model);
        }
    }
}
