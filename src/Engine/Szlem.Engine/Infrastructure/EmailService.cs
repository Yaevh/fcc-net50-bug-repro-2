using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;

namespace Szlem.Engine.Infrastructure
{
    public class EmailOptions
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class EmailMessage
    {
        public EmailAddress From { get; set; }
        public IReadOnlyCollection<EmailAddress> To { get; set; } = Array.Empty<EmailAddress>();
        public string Subject { get; set; }
        public string Body { get; set; }
        public IReadOnlyCollection<EmailAttachment> Attachments { get; set; } = Array.Empty<EmailAttachment>();
        public bool IsBodyHtml { get; set; } = true;

        internal EmailMessage() { }
    }

    public class EmailAttachment
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }

    public interface IEmailService
    {
        EmailMessage CreateMessage(EmailAddress to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null);
        Task<Result> Send(EmailMessage message, CancellationToken token);
    }

    public static class EmailServiceExtensions
    {
        public static EmailMessage CreateMessage(this IEmailService emailService, string to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null)
        {
            return emailService.CreateMessage(EmailAddress.Parse(to), subject, body, isBodyHtml, attachments);
        }

        public static Task<Result> TrySend(this IEmailService emailService, string to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null, CancellationToken cancellationToken = default)
        {
            var message = emailService.CreateMessage(to, subject, body, isBodyHtml, attachments);
            return emailService.Send(message, cancellationToken);
        }
    }


    public class MailKitEmailService : IEmailService
    {
        private readonly IOptions<EmailOptions> _options;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IOptions<EmailOptions> options, ILogger<MailKitEmailService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public EmailMessage CreateMessage(EmailAddress to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null)
        {
            Guard.Against.Default(to, nameof(to));
            Guard.Against.Null(subject, nameof(subject));
            Guard.Against.Null(body, nameof(body));

            if (EmailAddress.TryParse(_options.Value.Username, out var from) == false)
                throw new ApplicationException($"'From' address not specified, specify it in {nameof(EmailOptions)}");
            
            return new EmailMessage() {
                From = from,
                To = new[] { to },
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml,
                Attachments = attachments?.ToArray() ?? Array.Empty<EmailAttachment>()
            };
        }

        public async Task<Result> Send(EmailMessage message, CancellationToken token)
        {
            try
            {
                var options = _options.Value;
                var mimeMessage = Parse(message);

                using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    emailClient.Connect(options.Server, options.Port, true);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    await emailClient.AuthenticateAsync(options.Username, options.Password, token);
                    await emailClient.SendAsync(mimeMessage, token);
                    await emailClient.DisconnectAsync(true, token);
                }

                _logger.LogInformation($"sent e-mail to [{string.Join(", ", message.To)}], content: {message.Body.TruncateWithEllipsis(1000)}");
                return Result.Success();
            }
            catch (Exception ex) when (IsKnown(ex))
            {
                _logger.LogWarning($"failed to send e-mail to [{string.Join(", ", message.To)}], exception: {ex.ToString()}, content: {message.Body.TruncateWithEllipsis(1000)}");
                return Result.Failure("Failed to send e-mail");
            }
        }

        private MimeKit.MimeMessage Parse(EmailMessage message)
        {
            var from = MimeKit.InternetAddress.Parse(message.From);
            var to = message.To.Select(x => MimeKit.InternetAddress.Parse(x));

            var bb = new MimeKit.BodyBuilder();
            if (message.IsBodyHtml)
                bb.HtmlBody = message.Body;
            else
                bb.TextBody = message.Body;

            foreach (var attachment in message.Attachments)
                bb.Attachments.Add(attachment.Name, attachment.Content);

            return new MimeKit.MimeMessage(new[] { from }, to, message.Subject, bb.ToMessageBody());
        }

        private bool IsKnown(Exception ex)
        {
            return ex is MimeKit.ParseException
                || ex is MailKit.CommandException
                || ex is MailKit.ProtocolException
                || ex is MailKit.Security.AuthenticationException
                || ex is MailKit.Security.SaslException
                || ex is MailKit.Security.SslHandshakeException
                || ex is ArgumentException
                || ex is IOException
                || ex is System.Net.Sockets.SocketException
                || ex is OperationCanceledException;
        }
    }

    public class SucceedingEmailService : IEmailService
    {
        private readonly List<EmailMessage> _sentMessages = new List<EmailMessage>();
        public IReadOnlyCollection<EmailMessage> SentMessages => _sentMessages.AsReadOnly();

        public EmailMessage CreateMessage(EmailAddress to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null)
        {
            Guard.Against.Default(to, nameof(to));
            Guard.Against.Null(subject, nameof(subject));
            Guard.Against.Null(body, nameof(body));

            return new EmailMessage() {
                From = EmailAddress.Parse("dummy@example.com"),
                To = new[] { to },
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml,
                Attachments = attachments?.ToArray() ?? Array.Empty<EmailAttachment>()
            };
        }

        public Task<Result> Send(EmailMessage message, CancellationToken token)
        {
            _sentMessages.Add(message);
            return Task.FromResult(Result.Success());
        }
    }

    public class FailingEmailService : IEmailService
    {
        private readonly List<EmailMessage> _failedMessages = new List<EmailMessage>();
        public IReadOnlyCollection<EmailMessage> FailedMessages => _failedMessages.AsReadOnly();

        public EmailMessage CreateMessage(EmailAddress to, string subject, string body, bool isBodyHtml = true, IEnumerable<EmailAttachment> attachments = null)
        {
            Guard.Against.Default(to, nameof(to));
            Guard.Against.Null(subject, nameof(subject));
            Guard.Against.Null(body, nameof(body));

            return new EmailMessage() {
                From = EmailAddress.Parse("dummy@example.com"),
                To = new[] { to },
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml,
                Attachments = attachments?.ToArray() ?? Array.Empty<EmailAttachment>()
            };
        }

        public Task<Result> Send(EmailMessage message, CancellationToken token)
        {
            _failedMessages.Add(message);
            return Task.FromResult(Result.Failure("failed to send e-mail"));
        }
    }
}
