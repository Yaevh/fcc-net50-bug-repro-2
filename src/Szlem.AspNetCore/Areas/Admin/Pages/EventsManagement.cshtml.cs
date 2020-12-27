using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.EventStores.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Szlem.AspNetCore.Areas.Admin.Pages
{
    public class EventsManagementModel : PageModel
    {
        public const string PageName = "EventsManagement";
        public static readonly string Route = $"/{PageName}";

        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly IEventPersistence _eventPersistence;
        public EventsManagementModel(IEventJsonSerializer eventJsonSerializer, IEventPersistence eventPersistence)
        {
            _eventJsonSerializer = eventJsonSerializer ?? throw new ArgumentNullException(nameof(eventJsonSerializer));
            _eventPersistence = eventPersistence ?? throw new ArgumentNullException(nameof(eventPersistence));
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostExportEventsFromFilesStoreToCsv()
        {
            if ((_eventPersistence is FilesEventPersistence) == false)
                throw new ApplicationException($"{nameof(FilesEventPersistence)} not configured");

            var events = await _eventPersistence.LoadAllCommittedEvents(GlobalPosition.Start, 1000, CancellationToken.None);
            var result = events.CommittedDomainEvents
                .Cast<FilesEventPersistence.FileEventData>()
                .Select(x => new { Source = x, Deserialized = _eventJsonSerializer.Deserialize(x)} )
                .Select(x => new EventFlow.SQLite.EventStores.SQLiteEventPersistence.EventDataModel()
            {
                    GlobalSequenceNumber = x.Source.GlobalSequenceNumber,
                    BatchId = Guid.Parse(x.Deserialized.Metadata["batch_id"]),
                    AggregateId = x.Source.AggregateId,
                    AggregateName = x.Deserialized.Metadata["aggregate_name"],
                    Data = x.Source.Data,
                    Metadata = x.Source.Metadata,
                    AggregateSequenceNumber = x.Source.AggregateSequenceNumber
            });

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.StreamWriter(stream))
            using (var csv = new CsvHelper.CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentUICulture)))
            {
                csv.WriteRecords(result);
                csv.Flush();
                writer.Flush();
                return File(stream.ToArray(), "text/csv", "events.csv");
            }
        }
    }
}
