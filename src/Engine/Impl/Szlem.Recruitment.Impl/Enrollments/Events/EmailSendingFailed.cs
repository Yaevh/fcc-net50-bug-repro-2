using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using Newtonsoft.Json;
using Remotion.Linq.EagerFetching.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.EmailSendingFailed", 1)]
    internal class EmailSendingFailed : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public NodaTime.Instant Instant { get; }
        public EmailAddress From { get; }
        public string Subject { get; }
        public string Body { get; }
        public bool IsBodyHtml { get; }
        public IReadOnlyCollection<EmailAttachment> Attachments { get; }

        [JsonConstructor]
        public EmailSendingFailed(NodaTime.Instant instant, EmailAddress from, string subject, string body, bool isBodyHtml, IEnumerable<EmailAttachment> attachments)
        {
            Guard.Against.Default(instant, nameof(instant));
            Guard.Against.Null(from, nameof(from));
            Guard.Against.Null(subject, nameof(subject));
            Guard.Against.Null(body, nameof(body));
            Guard.Against.Null(attachments, nameof(attachments));
            Instant = instant;
            From = from;
            Subject = subject;
            Body = body;
            IsBodyHtml = isBodyHtml;
            Attachments = attachments.ToArray();
        }

        public EmailSendingFailed(NodaTime.Instant instant, EmailMessage message) : this(instant, message.From, message.Subject, message.Body, message.IsBodyHtml, message.Attachments) { }

        public override string ToString()
        {
            var content = $"FAILED to send {Subject} (from <{From}>): {Body.TruncateWithEllipsis(100)}";
            if (Attachments.Any())
                content = $"{content}; att: {string.Join(", ", Attachments.Select(x => x.Name))}";
            return content;
        }
    }
}
