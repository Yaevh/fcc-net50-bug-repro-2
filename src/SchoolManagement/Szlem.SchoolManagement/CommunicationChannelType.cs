using Ardalis.SmartEnum;
using EventFlow.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Szlem.SchoolManagement
{
    [Newtonsoft.Json.JsonConverter(typeof(Ardalis.SmartEnum.JsonNet.SmartEnumNameConverter<CommunicationChannelType, int>))]
    public class CommunicationChannelType : SmartEnum<CommunicationChannelType>
    {
        public static readonly CommunicationChannelType Unknown = new CommunicationChannelType(nameof(Unknown), 0, "brak danych");

        [Display(Name = "Telefon wychodzący (od koordynatora do szkoły)")]
        public static readonly CommunicationChannelType OutgoingPhone = new CommunicationChannelType(nameof(OutgoingPhone), 1, "Telefon wychodzący (od koordynatora do szkoły)");

        [Display(Name = "Email wychodzący (od koordynatora do szkoły)")]
        public static readonly CommunicationChannelType OutgoingEmail = new CommunicationChannelType(nameof(OutgoingEmail), 2, "Email wychodzący (od koordynatora do szkoły)");

        [Display(Name = "Kontakt osobisty wychodzący (z inicjatywy koordynatora)")]
        public static readonly CommunicationChannelType OutgoingPersonalContact = new CommunicationChannelType(nameof(OutgoingPersonalContact), 3, "Kontakt osobisty wychodzący (z inicjatywy koordynatora)");

        [Display(Name = "Telefon przychodzący (od szkoły do koordynatora)")]
        public static readonly CommunicationChannelType IncomingPhone = new CommunicationChannelType(nameof(IncomingPhone), 4, "Telefon przychodzący (od szkoły do koordynatora)");

        [Display(Name = "Email przychodzący (od szkoły do koordynatora)")]
        public static readonly CommunicationChannelType IncomingEmail = new CommunicationChannelType(nameof(IncomingEmail), 5, "Email przychodzący (od szkoły do koordynatora)");

        [Display(Name = "Kontakt osobisty przychodzący (z inicjatywy szkoły)")]
        public static readonly CommunicationChannelType IncomingPersonalContact = new CommunicationChannelType(nameof(IncomingPersonalContact), 6, "Kontakt osobisty przychodzący (z inicjatywy szkoły)");

        private CommunicationChannelType(string name, int value, string displayName) : base(name, value) => DisplayName = displayName;

        public string DisplayName { get; }

        public bool IsEmail => this == IncomingEmail || this == OutgoingEmail;
        public bool IsPhone => this == IncomingPhone || this == OutgoingPhone;
        public bool IsPersonal => this == IncomingPersonalContact || this == OutgoingPersonalContact;

        public bool IsIncoming => this == IncomingEmail || this == IncomingPhone || this == IncomingPersonalContact;
        public bool IsOutgoing => this == OutgoingEmail || this == OutgoingPhone || this == OutgoingPersonalContact;

        public override string ToString() => DisplayName;
    }
}
