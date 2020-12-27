using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Szlem.Domain;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class GetEnrollmentDetails
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class QueryByEnrollmentId : IRequest<Result<Details, Error>>
        {
            public Guid EnrollmentID { get; set; }
        }

        [Authorize(AuthorizationPolicies.OwningCandidateOrCoordinator)]
        public class QueryByEmail : IRequest<Result<Details, Error>>
        {
            public EmailAddress Email { get; set; }
        }

        public class Details
        {
            public Guid ID { get; set; }
            [Display(Name = "Data zgłoszenia")] public NodaTime.ZonedDateTime SubmissionDateTime { get; set; }
            [Display(Name = "Imię")] public string FirstName { get; set; }
            [Display(Name = "Nazwisko")] public string LastName { get; set; }
            [Display(Name = "Imię i nazwisko")] public string FullName { get; set; }
            [Display(Name = "Email")] public EmailAddress Email { get; set; }
            [Display(Name = "Telefon")] public PhoneNumber PhoneNumber { get; set; }
            [Display(Name = "Region")] public string Region { get; set; }
            [Display(Name = "Preferowane miasta prowadzenia zajęć")] public IReadOnlyCollection<string> PreferredLecturingCities { get; set; }
            [Display(Name = "Preferowane szkolenia")] public IReadOnlyCollection<TrainingSummary> PreferredTrainings { get; set; }
            [Display(Name = "Wybrane szkolenie")] public TrainingSummary SelectedTraining { get; set; }
            [Display(Name = "Zdarzenia")] public IReadOnlyCollection<EventData> Events { get; set; }
            [Display(Name = "Czy kandydata można zaprosić na szkolenie?")] public bool CanInviteToTraining { get; set; }
            [Display(Name = "Czy kandydat może odmówić zaproszenia na szkolenie?")] public bool CanRefuseTrainingInvitation { get; set; }
            [Display(Name = "Czy można zarejestrować wyniki szkolenia?")] public bool CanRecordTrainingResults { get; set; }
            [Display(Name = "Czy kandydat może zrezygnować?")] public bool CanResign { get; set; }
            [Display(Name = "Czy kandydat zrezygnował?")] public bool HasResigned { get; set; }
            [Display(Name = "Czy kandydat ma uprawnienia prowadzącego?")] public bool HasLecturerRights { get; set; }
            [Display(Name = "Czy kandydat zgłosił się w bieżącej edycji?")] public bool IsCurrentSubmission { get; set; }
            [Display(Name = "Czy zgłoszenie pochodzi ze starej edycji?")] public bool IsOldSubmission { get; set; }
            [Display(Name = "Czy kandydat trwale zrezygnował z udziału w projekcie?")] public bool HasResignedPermanently { get; set; }
            [Display(Name = "Czy kandydat tymczasowo zrezygnował z udziału w projekcie?")] public bool HasResignedTemporarily { get; set; }
            [Display(Name = "Przewidywana data wznowienia działalności w przypadku tymczasowej rezygnacji")] public NodaTime.LocalDate? ResumeDate { get; set; }
        }


        [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftEventDataConverter))]
        public abstract class EventData
        {
            [Display(Name = "Czas zdarzenia")] public NodaTime.ZonedDateTime DateTime { get; set; }
        }


        public class RecruitmentFormSubmittedEventData : EventData
        {
            [Display(Name = "Imię i nazwisko")] public string FullName { get; set; }
            [Display(Name = "Email")] public string EmailAddress { get; set; }
            [Display(Name = "Telefon")] public string PhoneNumber { get; set; }
            [Display(Name = "Preferowane szkolenia")] public IReadOnlyCollection<TrainingSummary> PreferredTrainings { get; set; }
            [Display(Name = "Preferowane miasta prowadzenia zajęć")] public IReadOnlyCollection<string> PreferredLecturingCities { get; set; }
            [Display(Name = "O mnie")] public string AboutMe { get; set; }
        }

        public class EmailSentEventData : EventData
        {
            [Display(Name = "Adresat")] public string To { get; set; }
            [Display(Name = "Tytuł")] public string Subject { get; set; }
            [Display(Name = "Treść")] public string Body { get; set; }
            [Display(Name = "Czy treść jest w formacie HTML?")] public bool IsBodyHtml { get; set; }
        }

        public class EmailFailedToSendEventData : EventData
        {
            [Display(Name = "Adresat")] public string To { get; set; }
            [Display(Name = "Tytuł")] public string Subject { get; set; }
            [Display(Name = "Treść")] public string Body { get; set; }
            [Display(Name = "Czy treść jest w formacie HTML?")] public bool IsBodyHtml { get; set; }
        }

        public class CandidateAcceptedTrainingInvitationEventData : EventData
        {
            [Display(Name = "Osoba zapraszająca")] public string RecordingUser { get; set; }
            [Display(Name = "Wybrane szkolenie")] public TrainingSummary SelectedTraining { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class CandidateRefusedTrainingInvitationEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca odmowę")] public string RecordingUser { get; set; }
            [Display(Name = "Powód odmowy")] public string RefusalReason { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class CandidateAttendedTrainingEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca obecność")] public string RecordingUser { get; set; }
            [Display(Name = "Szkolenie")] public TrainingSummary Training { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class CandidateWasAbsentFromTrainingEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca nieobecność")] public string RecordingUser { get; set; }
            [Display(Name = "Szkolenie")] public TrainingSummary Training { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class CandidateResignedPermanentlyEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca rezygnację")] public string RecordingUser { get; set; }
            [Display(Name = "Powód rezygnacji")] public string ResignationReason { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class CandidateResignedTemporarilyEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca rezygnację")] public string RecordingUser { get; set; }
            [Display(Name = "Powód rezygnacji")] public string ResignationReason { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
            [Display(Name = "Data wznowienia działalności")] public NodaTime.LocalDate? ResumeDate { get; set; }
        }

        public class CandidateObtainedLecturerRightsEventData : EventData
        {
            [Display(Name = "Osoba nadająca uprawnienia")] public string RecordingUser { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }

        public class ContactOccuredEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca kontakt")] public string RecordingUser { get; set; }
            [Display(Name = "Kanał komunikacji")] public CommunicationChannel CommunicationChannel { get; set; }
            [Display(Name = "Treść")] public string Content { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; }
        }


        private class NewtonsoftEventDataConverter : Newtonsoft.Json.JsonConverter<EventData>
        {
            public override EventData ReadJson(JsonReader reader, Type objectType, [AllowNull] EventData existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
                var typeName = jObject["type"].ToString();

                var candidateNamespace = this.GetType().FullName.TrimEnd(nameof(NewtonsoftEventDataConverter));
                var result = Activator.CreateInstance(Type.GetType($"{candidateNamespace}{typeName}EventData")) as EventData;

                serializer.Populate(jObject.CreateReader(), result);
                return result;
            }

            public override void WriteJson(JsonWriter writer, [AllowNull] EventData value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                var jObject = new Newtonsoft.Json.Linq.JObject();
                jObject["type"] = value.GetType().Name.TrimEnd("EventData");
                foreach (var property in value.GetType().GetProperties())
                {
                    var propValue = property.GetValue(value);
                    if (propValue == null)
                        continue;
                    jObject[property.Name] = Newtonsoft.Json.Linq.JToken.FromObject(propValue, serializer);
                }
                jObject.WriteTo(writer);
            }
        }
    }
}
