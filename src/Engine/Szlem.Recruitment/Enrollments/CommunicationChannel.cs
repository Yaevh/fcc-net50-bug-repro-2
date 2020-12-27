using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Szlem.Recruitment.Enrollments
{
    public enum CommunicationChannel
    {
        Unknown = 0,
        [Display(Name = "Telefon wychodzący (od koordynatora do kandydata)")]
        OutgoingPhone = 1,
        [Display(Name = "Email wychodzący (od koordynatora do kandydata)")]
        OutgoingEmail = 2,
        [Display(Name = "Kontakt osobisty wychodzący (z inicjatywy koordynatora)")]
        OutgoingPersonalContact = 3,
        [Display(Name = "Telefon przychodzący (od kandydata do koordynatora)")]
        IncomingPhone = 4,
        [Display(Name = "Email przychodzący (od kandydata do koordynatora)")] 
        IncomingEmail = 5,
        [Display(Name = "Kontakt osobisty przychodzący (z inicjatywy kandydata)")]
        IncomingPersonalContact = 6,
        [Display(Name = "Żądanie API przychodzące (złożone przez kandydata)")]
        IncomingApiRequest = 7
    }
}
