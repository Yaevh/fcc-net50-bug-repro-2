using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.SchoolManagement
{
    public static class GetDetails
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Result<SchoolDetails, Error>>
        {
            public Guid SchoolId { get; set; }
        }


        public class SchoolDetails
        {
            public Guid Id { get; set; }
            [Display(Name = "Nazwa szkoły")] public string Name { get; set; } = string.Empty;
            [Display(Name = "Miasto")] public string City { get; set; } = string.Empty;
            [Display(Name = "Adres")] public string Address { get; set; } = string.Empty;
            [Display(Name = "Dane kontaktowe")] public IReadOnlyCollection<ContactData> ContactData { get; set; } = Array.Empty<ContactData>();
            [Display(Name = "Zdarzenia")] public IReadOnlyCollection<EventData> Events { get; set; } = Array.Empty<EventData>();
            [Display(Name = "Notatki")] public IReadOnlyCollection<NoteData> Notes { get; set; } = Array.Empty<NoteData>();
            public bool HasSignedPermanentAgreement { get; set; }
            public bool HasSignedFixedTermAgreement { get; set; }
            public LocalDate? AgreementEndDate { get; set; }
            public bool HasAgreedInitially { get; set; }
            public bool HasResignedPermanently { get; set; }
            public bool HasResignedTemporarily { get; set; }
            public LocalDate? ResignationEndDate { get; set; }
        }


        #region EventData
        [JsonConverter(typeof(NewtonsoftEventDataConverter))]
        public abstract class EventData
        {
            [Display(Name = "Czas zdarzenia")] public OffsetDateTime DateTime { get; set; }
        }

        public class SchoolRegisteredEventData : EventData { }

        public class ContactOccuredEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca kontakt")] public string RecordingUser { get; set; } = string.Empty;
            [Display(Name = "Kanał komunikacji")] public CommunicationChannelType CommunicationChannel { get; set; } = CommunicationChannelType.Unknown;
            [Display(Name = "Osoba z którą doszło do kontaktu")] public string ContactPersonName { get; set; } = string.Empty;
            [Display(Name = "Adres e-mail kontaktu")] public EmailAddress EmailAddress { get; set; }
            [Display(Name = "Numer telefonu kontaktu")] public PhoneNumber PhoneNumber { get; set; }
            [Display(Name = "Treść")] public string Content { get; set; } = string.Empty;
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; } = string.Empty;
        }

        public class InitialAgreementAchievedEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca zgodę")] public string RecordingUser { get; set; } = string.Empty;
            [Display(Name = "Osoba wyrażająca zgodę")] public string AgreeingPersonName { get; set; } = string.Empty;
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; } = string.Empty;
        }

        public class FixedTermAgreementSignedEventData : EventData
        {
            public Guid AgreementId { get; set; }
            [Display(Name = "Osoba rejestrująca kontakt")] public string RecordingUser { get; set; } = string.Empty;
            [Display(Name = "Data zakończenia umowy")] public NodaTime.LocalDate AgreementEndDate { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; } = string.Empty;
        }

        public class PermanentAgreementSignedEventData : EventData
        {
            public Guid AgreementId { get; set; }
            [Display(Name = "Osoba rejestrująca kontakt")] public string RecordingUser { get; set; } = string.Empty;
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; } = string.Empty;
        }

        public class SchoolResignedFromCooperationEventData : EventData
        {
            [Display(Name = "Osoba rejestrująca kontakt")] public string RecordingUser { get; set; } = string.Empty;
            [Display(Name = "Data potencjalnego następnego kontaktu")] public NodaTime.LocalDate? PotentialNextContactDate { get; set; }
            [Display(Name = "Notatki i dodatkowe informacje")] public string AdditionalNotes { get; set; } = string.Empty;
        }


        private class NewtonsoftEventDataConverter : JsonConverter<EventData>
        {
            public override EventData ReadJson(JsonReader reader, Type objectType, [AllowNull] EventData existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var typeName = jObject["type"].ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                var candidateNamespace = this.GetType().FullName.TrimEnd(nameof(NewtonsoftEventDataConverter));
                var result = (EventData)Activator.CreateInstance(Type.GetType($"{candidateNamespace}{typeName}EventData"));

                serializer.Populate(jObject.CreateReader(), result);
                return result;
            }

            public override void WriteJson(JsonWriter writer, [NotNull] EventData value, JsonSerializer serializer)
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
        #endregion

        public class NoteData
        {
            public Guid Id { get; set; }
            public string Content { get; set; }
            public OffsetDateTime CreatedAt { get; set; }
            public bool WasEdited { get; set; }
            public OffsetDateTime? EditedAt { get; set; }
            public string Author { get; set; }
        }
    }
}
#nullable restore