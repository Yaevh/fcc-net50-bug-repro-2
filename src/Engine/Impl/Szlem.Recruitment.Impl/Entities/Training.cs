using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Recruitment.Trainings;

namespace Szlem.Recruitment.Impl.Entities
{
    public class Training
    {
        public virtual int ID { get; protected set; }

        public virtual NodaTime.OffsetDateTime StartDateTime { get; protected set; }

        public virtual NodaTime.OffsetDateTime EndDateTime { get; protected set; }

        public virtual Guid CoordinatorID { get; protected set; }

        public virtual string City { get; protected set; }

        public virtual string Address { get; protected set; }

        public virtual NodaTime.Duration Duration => EndDateTime - StartDateTime;

        #region Notes
#nullable enable
        private readonly IList<Note> _notes = new List<Note>();
        public virtual IReadOnlyCollection<Note> Notes => _notes.ToList().AsReadOnly();

        public virtual Result AddNote(Guid authorId, string content, NodaTime.Instant timestamp)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure(ErrorMessages.NoteCannotBeEmpty);
            _notes.Add(new Note(this, authorId, content, timestamp));
            return Result.Success();
        }
#nullable restore
        #endregion

        public virtual Campaign Campaign { get; protected internal set; }

        /// <summary>
        /// for NHibernate only
        /// </summary>
        protected Training() { }


        public Training(string address, string city, NodaTime.OffsetDateTime startDateTime, NodaTime.OffsetDateTime endDateTime, Guid coordinatorId)
        {
            Guard.Against.NullOrWhiteSpace(address, nameof(address));
            Guard.Against.NullOrWhiteSpace(city, nameof(city));
            Guard.Against.Default(startDateTime, nameof(startDateTime));
            Guard.Against.Default(endDateTime, nameof(endDateTime));
            Guard.Against.Default(coordinatorId, nameof(coordinatorId));
            
            Address = address;
            City = city;
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
            CoordinatorID = coordinatorId;
        }

        public virtual TrainingTiming CalculateTiming(NodaTime.Instant now)
        {
            if (now < StartDateTime.ToInstant())
                return TrainingTiming.Future;
            else if (EndDateTime.ToInstant() < now)
                return TrainingTiming.Past;
            else
                return TrainingTiming.Current;
        }

        public class Note
        {
            public virtual int Id { get; protected set; }
            public virtual Guid AuthorId { get; protected set; }
            public virtual string Content { get; protected set; }
            public virtual NodaTime.Instant Timestamp { get; protected set; }
            public virtual Training Training { get; protected set; }

            protected Note() { } // for NHibernate only

            internal Note(Training training, Guid authorId, string content, NodaTime.Instant timestamp)
            {
                Guard.Against.Null(training, nameof(training));
                Guard.Against.Default(authorId, nameof(authorId));
                Guard.Against.NullOrWhiteSpace(content, nameof(content));
                Guard.Against.Default(timestamp, nameof(timestamp));
                Training = training;
                AuthorId = authorId;
                Content = content;
                Timestamp = timestamp;
            }
        }

        public class Validator : AbstractValidator<Training>
        {
            public Validator()
            {
                RuleFor(x => x.ID).NotEmpty();
                RuleFor(x => x.StartDateTime).NotEmpty();
                RuleFor(x => x.EndDateTime).NotEmpty();
                RuleFor(x => x.StartDateTime.ToInstant()).LessThan(x => x.EndDateTime.ToInstant()).WithName(nameof(StartDateTime));
                RuleFor(x => x.CoordinatorID).NotEmpty();
                RuleFor(x => x.City).NotEmpty();
                RuleFor(x => x.Address).NotEmpty();
                RuleFor(x => x.Campaign).NotEmpty();
            }
        }
    }
}
