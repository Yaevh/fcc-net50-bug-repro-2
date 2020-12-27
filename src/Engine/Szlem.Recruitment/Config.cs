using FluentValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Szlem.SharedKernel;

namespace Szlem.Recruitment
{
    public class Config
    {
        public EnvironmentVariableString DbConnectionString { get; set; }

        /// <summary>
        /// Set to true to keep DB session open at all times; useful in testing, must be false in production
        /// </summary>
        public bool KeepDbSessionOpen { get; set; } = false;

        public bool AllowBreakingMigrations { get; set; } = false;

        public EmailMessageConfig GreetingEmail { get; set; } = new EmailMessageConfig();

        public EmailMessageConfig TrainingReminderEmail { get; set; } = new EmailMessageConfig();

        public class EmailMessageConfig
        {
            public string Subject { get; set; }
            public string Body { get; set; }
            public EnvironmentVariableString BodySourceFile { get; set; }
            public bool IsBodyHtml { get; set; }

            public async Task<string> BuildMessageBody()
            {
                if (Body.IsNullOrEmpty() == false)
                    return Body;
                using (var file = new StreamReader(File.OpenRead(BodySourceFile)))
                    return await file.ReadToEndAsync();
            }

            public class Validator : AbstractValidator<EmailMessageConfig>
            {
                public Validator()
                {
                    RuleFor(x => x.Subject).NotEmpty();

                    RuleFor(x => x.Body).Empty()
                        .When(x => string.IsNullOrEmpty(x.BodySourceFile) == false)
                        .WithMessage($"{nameof(Body)} cannot be set then {nameof(BodySourceFile)} is set");
                    RuleFor(x => x.BodySourceFile).Must(x => string.IsNullOrEmpty(x))
                        .When(x => string.IsNullOrEmpty(x.Body) == false)
                        .WithMessage($"{nameof(BodySourceFile)} cannot be set then {nameof(Body)} is set");

                    RuleFor(x => x.Body).NotEmpty()
                        .When(x => string.IsNullOrEmpty(x.BodySourceFile))
                        .WithMessage($"Either {nameof(Body)} or {nameof(BodySourceFile)} must be set");
                    RuleFor(x => x.BodySourceFile).Must(x => string.IsNullOrEmpty(x) == false)
                        .When(x => string.IsNullOrEmpty(x.Body)).WithMessage($"Either {nameof(BodySourceFile)} or {nameof(Body)} must set"); ;
                }
            }
        }

        public class Validator : AbstractValidator<Config>
        {
            public Validator()
            {
                RuleFor(x => x.DbConnectionString).NotEmpty();
                RuleFor(x => x.GreetingEmail).SetValidator(new EmailMessageConfig.Validator());
                RuleFor(x => x.TrainingReminderEmail).SetValidator(new EmailMessageConfig.Validator());
            }
        }
    }
}
