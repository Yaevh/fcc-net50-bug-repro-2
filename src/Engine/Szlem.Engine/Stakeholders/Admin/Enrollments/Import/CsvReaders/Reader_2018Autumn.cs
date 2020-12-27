using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Szlem.Domain;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import.CsvReaders
{
    public class Reader_2018Autumn : EnrollmentCsvReaderBase, IEnrollmentCsvReader
    {
        private static readonly string[] _headers = new string[] { "name", "surname", "mail", "city", "your_knowledge", "your_experience", "how_did_you_know_us", "rodo", "phone" };

        public override string[] Headers => _headers;


        public override Request.Submission ReadRecord(CsvReader reader)
        {
            var recordDefinition = new
            {
                name = string.Empty,
                surname = string.Empty,
                mail = string.Empty,
                city = string.Empty,
                your_knowledge = string.Empty,
                your_experience = string.Empty,
                how_did_you_know_us = string.Empty,
                rodo = string.Empty,
                phone = string.Empty
            };
            var record = reader.GetRecord(recordDefinition);
            
            return new Request.Submission()
            {
                FirstName = record.name.Trim(),
                LastName = record.surname.Trim(),
                Email = EmailAddress.Create(record.mail.Trim()).Value,
                PhoneNumber = string.IsNullOrWhiteSpace(record.phone) ? null : PhoneNumber.Create(record.phone.Trim()).Value,
                Cities = record.city
                    .Split(new[] { ",", ";", "/" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToList(),
                Province = "N/A",
                AboutMe = $"{record.your_knowledge.Trim()}\n{record.your_experience.Trim()}\n{record.how_did_you_know_us.Trim()}"
            };
        }
    }
}
