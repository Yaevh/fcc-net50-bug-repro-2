using EventFlow.ReadStores;
using NHibernate.Persister.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal interface IEnrollmentRepository
    {
        Task Insert(EnrollmentReadModel entry);
        Task Update(EnrollmentReadModel entry);
        IQueryable<EnrollmentReadModel> Query();
    }

    internal class EnrollmentReadModel : IReadModel
    {
        public EnrollmentAggregate.EnrollmentId Id { get; set; }
        public NodaTime.Instant Timestamp { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public EmailAddress Email { get; set; }
        public PhoneNumber PhoneNumber { get; set; }
        public string AboutMe { get; set; }
        public CampaignSummary Campaign { get; set; }
        public string Region { get; set; }
        public IReadOnlyCollection<string> PreferredLecturingCities { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<TrainingSummary> PreferredTrainings { get; set; } = Array.Empty<TrainingSummary>();
        public TrainingSummary SelectedTraining { get; set; }
        public bool HasRefusedTraining { get; set; }
        public string TrainingRefusalReason { get; set; }
        public bool HasLecturerRights { get; set; }
        public bool HasResignedPermanently { get; set; }
        public bool HasResignedTemporarily { get; set; }

        public bool HasResignedTemporarilyAsOf(NodaTime.Instant now) => HasResignedTemporarily && (ResumeDate == null || ResumeDate.Value >= now.InMainTimezone().Date);
        public bool CanReportTrainingResultsConditionally { get; set; }
        public bool CanReportTrainingResults { get; set; }

        /// <summary>
        /// Data wznowienia uczestnictwa po tymczasowej rezygnacji
        /// </summary>
        public NodaTime.LocalDate? ResumeDate { get; set; }

        public Recruitment.Enrollments.RecordTrainingResults.TrainingResult? TrainingResult { get; set; }

        public class CampaignSummary
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
        }

        public class TrainingSummary
        {
            public int ID { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
            public Guid CoordinatorID { get; set; }
            public string City { get; set; }
            public string Address { get; set; }
        }
    }

    internal class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<EnrollmentAggregate.EnrollmentId, EnrollmentReadModel> _repo = new System.Collections.Concurrent.ConcurrentDictionary<EnrollmentAggregate.EnrollmentId, EnrollmentReadModel>();

        public Task Insert(EnrollmentReadModel entry)
        {
            _repo[entry.Id] = entry;
            return Task.CompletedTask;
        }

        public Task Update(EnrollmentReadModel entry)
        {
            _repo[entry.Id] = entry;
            return Task.CompletedTask;
        }

        public IQueryable<EnrollmentReadModel> Query()
        {
            return _repo.Values.AsQueryable();
        }
    }
}
