using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import.CsvReaders
{
    public abstract class EnrollmentCsvReaderBase : IEnrollmentCsvReader
    {
        public abstract string[] Headers { get; }

        public abstract Import.Request.Submission ReadRecord(CsvReader reader);


        protected string ReadField(CsvReader reader, string header)
        {
            return reader.GetField(Array.IndexOf(Headers, header));
        }
    }
}
