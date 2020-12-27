using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;

#nullable enable
namespace Szlem.SchoolManagement
{
    public class ContactData
    {
        [Display(Name = "Imię i nazwisko lub nazwa kontaktu", Prompt = "np. \"sekretariat\", \"dyrektor Jan Kowalski\"")] public string? Name { get; set; }

        [Display(Name = "Adres e-mail")] public EmailAddress? EmailAddress { get; set; }

        [Display(Name = "Nr telefonu")] public PhoneNumber? PhoneNumber { get; set; }

        [Display(Name = "Uwagi i komentarze")] public string? Comment { get; set; }
    }
}
#nullable restore
