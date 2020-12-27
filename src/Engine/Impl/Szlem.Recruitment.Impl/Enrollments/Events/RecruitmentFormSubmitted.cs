using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.Recruitment.Enrollments;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.RecruitmentFormSubmitted", 1)]
    internal class RecruitmentFormSubmitted : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Instant SubmissionDate { get; }

        public string FirstName { get; }
        public string LastName { get; }

        public EmailAddress Email { get; }
        public PhoneNumber PhoneNumber { get; }

        public string AboutMe { get; }

        public int CampaignID { get; }
        public string Region { get; }
        public IReadOnlyCollection<string> PreferredLecturingCities { get; }
        public IReadOnlyCollection<int> PreferredTrainingIds { get; }

        public bool GdprConsentGiven { get; }


        public RecruitmentFormSubmitted(
            Instant submissionDate,
            string firstName,
            string lastName,
            EmailAddress email,
            PhoneNumber phoneNumber,
            string aboutMe,
            int campaignID,
            string region,
            IReadOnlyCollection<string> preferredLecturingCities,
            IReadOnlyCollection<int> preferredTrainingIds,
            bool gdprConsentGiven)
        {
            Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
            Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
            Guard.Against.Null(email, nameof(email));
            Guard.Against.Null(phoneNumber, nameof(phoneNumber));
            Guard.Against.NullOrWhiteSpace(aboutMe, nameof(aboutMe));
            Guard.Against.Default(campaignID, nameof(campaignID));
            Guard.Against.NullOrWhiteSpace(region, nameof(region));
            Guard.Against.NullOrEmpty(preferredLecturingCities, nameof(preferredLecturingCities));
            Guard.Against.NullOrEmpty(preferredTrainingIds, nameof(preferredTrainingIds));
            Guard.Against.False(gdprConsentGiven, nameof(gdprConsentGiven));

            if (preferredLecturingCities.Distinct().Count() != preferredLecturingCities.Count)
                throw new InvalidOperationException($"duplicates found in {nameof(preferredLecturingCities)}");
            if (preferredTrainingIds.Distinct().Count() != preferredTrainingIds.Count)
                throw new InvalidOperationException($"duplicates found in {nameof(preferredTrainingIds)}");

            SubmissionDate = submissionDate;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            AboutMe = aboutMe;
            CampaignID = campaignID;
            Region = region;
            PreferredLecturingCities = preferredLecturingCities;
            PreferredTrainingIds = preferredTrainingIds;
            GdprConsentGiven = gdprConsentGiven;
        }

        public static RecruitmentFormSubmitted From(SubmitRecruitmentForm.Command command, Instant submissionDate, int campaignID)
        {
            return new RecruitmentFormSubmitted(
                submissionDate,
                command.FirstName,
                command.LastName,
                command.Email,
                command.PhoneNumber,
                command.AboutMe,
                campaignID,
                command.Region,
                command.PreferredLecturingCities,
                command.PreferredTrainingIds,
                command.GdprConsentGiven);
        }
    }
}
