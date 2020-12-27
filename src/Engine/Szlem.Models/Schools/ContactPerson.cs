using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text;
using Szlem.Domain;

namespace Szlem.Models.Schools
{
    [Obsolete]
    public class ContactPerson
    {
        public int ID { get; set; }

        /// <summary>
        /// Imię i nazwisko
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Stanowisko
        /// </summary>
        public string Position { get; set; }

        public string Email { get; set; }

        public PhoneNumber PhoneNumber { get; set; }
    }
}
