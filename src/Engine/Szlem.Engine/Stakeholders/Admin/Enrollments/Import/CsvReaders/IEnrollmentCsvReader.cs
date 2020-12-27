using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import.CsvReaders
{
    public interface IEnrollmentCsvReader
    {
        string[] Headers { get; }

        Request.Submission ReadRecord(CsvReader reader);
    }
}
