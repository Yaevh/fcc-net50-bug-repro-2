//using CsvHelper;
//using MediatR;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Szlem.Engine.Stakeholders.Admin.Enrollments.Import.CsvReaders;
//using Szlem.Domain.Exceptions;

//namespace Szlem.Engine.Stakeholders.Admin.Enrollments
//{
//    public class ImportFromCsvHandler : IRequestHandler<Import.RequestFromCsv>
//    {
//        private readonly IEnrollmentCsvReader[] _readingStrategies = new IEnrollmentCsvReader[] {
//            new Reader_2016Autumn(), new Reader_2017Spring(), new Reader_2017Autumn2018Spring(), new Reader_2018Autumn()
//        };
//        private readonly CsvHelper.Configuration.Configuration _csvConfig = new CsvHelper.Configuration.Configuration {
//            Delimiter = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ListSeparator
//        };

//        private readonly IRequestHandler<Import.Request, Unit> _handlerImpl;

//        public ImportFromCsvHandler(IRequestHandler<Import.Request, Unit> handlerImpl)
//        {
//            _handlerImpl = handlerImpl ?? throw new ArgumentNullException(nameof(handlerImpl));
//        }

//        public async Task<Unit> Handle(Import.RequestFromCsv request, CancellationToken cancellationToken)
//        {
//            using (var reader = new StreamReader(request.TextContent))
//            {
//                using (var csvReader = new CsvReader(reader, new CsvHelper.Configuration.Configuration() { Delimiter = "," }))
//                {
//                    await csvReader.ReadAsync();
//                    csvReader.ReadHeader();
                    
//                    var lineReader = _readingStrategies.SingleOrDefault(
//                        x => x.Headers.SequenceEqual(csvReader.Context.Record.TakeWhile(y => !string.IsNullOrEmpty(y)))
//                    );
//                    if (lineReader == null)
//                        throw new SzlemException("could not find appropriate parser based on CSV header");

//                    return await HandleImpl(request, csvReader, lineReader, cancellationToken);
//                }
//            }
//        }

//        private async Task<Unit> HandleImpl(Import.RequestFromCsv request, CsvReader reader, IEnrollmentCsvReader lineReader, CancellationToken cancellationToken)
//        {
//            var submissions = new List<Import.Request.Submission>();
//            var errorCount = 0;
//            while (await reader.ReadAsync())
//            {
//                try
//                {
//                    submissions.Add(lineReader.ReadRecord(reader));
//                }
//                catch (PhoneNumbers.NumberParseException) { ++errorCount; }
//                catch (FormatException) { ++errorCount; }
//                catch (InvalidOperationException) { ++errorCount; }
//            }
                
//            var subRequest = new Import.Request()
//            {
//                RecruitmentCampaignID = request.RecruitmentCampaignID,
//                Submissions = submissions
//            };
//            return await _handlerImpl.Handle(subRequest, cancellationToken);
//        }
//    }
//}
