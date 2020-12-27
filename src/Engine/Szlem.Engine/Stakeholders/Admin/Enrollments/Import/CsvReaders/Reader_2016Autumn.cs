using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Szlem.Domain;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import.CsvReaders
{
    public class Reader_2016Autumn : EnrollmentCsvReaderBase, IEnrollmentCsvReader
    {
        private static readonly string[] _headers = new string[] { "Sygnatura czasowa", "Imię i Nazwisko", "Adres e-mail", "Telefon", "Miasto", "Województwo", "Kilka zdań o sobie", "Czego oczekujesz od projektu" };

        public override string[] Headers => _headers;


        public override Request.Submission ReadRecord(CsvReader reader)
        {
            var enrollment = new Request.Submission()
            {
                FirstName = ReadField(reader, "Imię i Nazwisko").Split(' ').First(),
                LastName = ReadField(reader, "Imię i Nazwisko").Split(' ').Skip(1).First(),
                Email = EmailAddress.Create(ReadField(reader, "Adres e-mail").Trim()).Value,
                PhoneNumber = PhoneNumber.Create(ReadField(reader, "Telefon").Trim()).Value,
                Cities = ReadField(reader, "Miasto")
                    .Split(new[] { ",", ";", "/" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToList(),
                Province = ReadField(reader, "Województwo"),
                AboutMe = ReadField(reader, "Kilka zdań o sobie"),
                WhatDoYouExpectFromTheProject = ReadField(reader, "Czego oczekujesz od projektu")
            };

            var dateFieldRaw = ReadField(reader, "Sygnatura czasowa");
            if (DateTime.TryParse(dateFieldRaw, out DateTime date))
                enrollment.SubmissionDate = date;

            return enrollment;
        }
    }
}
