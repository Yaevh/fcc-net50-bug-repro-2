using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Recruitment.Trainings;

namespace Szlem.Recruitment.Impl.Entities
{
    public class Campaign
    {
        /// <summary>
        /// for NHibernate only
        /// </summary>
        protected Campaign() { }


        public Campaign(NodaTime.OffsetDateTime startDateTime, NodaTime.OffsetDateTime endDateTime, int editionId, string name = null)
        {
            if (startDateTime == default)
                throw new ArgumentException($"{nameof(startDateTime)} cannot be empty", nameof(startDateTime));
            StartDateTime = startDateTime;

            if (endDateTime == default)
                throw new ArgumentException($"{nameof(endDateTime)} cannot be empty", nameof(endDateTime));
            EndDateTime = endDateTime;

            if (startDateTime.ToInstant() > endDateTime.ToInstant())
                throw new InvalidOperationException($"{nameof(startDateTime)} must be earlier than {nameof(endDateTime)}");

            if (editionId == default)
                throw new ArgumentException($"{nameof(editionId)} cannot be empty", nameof(editionId));
            EditionId = editionId;

            Name = name;
        }

        public virtual int Id { get; protected set; }

        public virtual NodaTime.OffsetDateTime StartDateTime { get; protected set; }

        public virtual NodaTime.OffsetDateTime EndDateTime { get; protected set; }

        public virtual string Name { get; protected set; }

        public virtual int EditionId { get; protected set; }


        private readonly ISet<Training> _trainings = new HashSet<Training>();
        public virtual IReadOnlyCollection<Training> Trainings => _trainings.ToList().AsReadOnly();

        public virtual NodaTime.Interval Interval => new NodaTime.Interval(StartDateTime.ToInstant(), EndDateTime.ToInstant());

        public virtual Result ScheduleTraining(Training scheduledTraining)
        {
            if (scheduledTraining.StartDateTime.ToInstant() < EndDateTime.ToInstant())
                return Result.Failure(ErrorMessages.ScheduledTrainingMustOccurAfterCampaignEnd);
            if (_trainings.Contains(scheduledTraining))
                throw new Domain.Exceptions.ValidationException($"{nameof(Campaign)} already contains this {nameof(Training)}");

            _trainings.Add(scheduledTraining);
            scheduledTraining.Campaign = this;
            return Result.Success();
        }
    }
}
