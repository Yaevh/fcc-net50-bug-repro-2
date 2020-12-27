using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Szlem.Domain;
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages.Enrollments
{
    public class IndexModel : PageModel
    {
        public const string PageName = "Index";
        public static readonly string Route = Consts.EnrollmentPageRoute(PageName);

        private readonly ISzlemEngine _engine;

        public IndexModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [FromQuery]
        public GetSubmissions.Query Query { get; set; }

        public IPagedList<GetSubmissions.SubmissionSummary> Submissions { get; set; }
        public IReadOnlyCollection<Szlem.Recruitment.Campaigns.Index.CampaignSummary> Campaigns { get; set; }
        public IReadOnlyCollection<Szlem.Recruitment.Trainings.TrainingSummary> Trainings { get; set; }

        

        public async Task<IActionResult> OnGet()
        {
            Submissions = await _engine.Query(Query);
            var campaigns = await _engine.Query(new Szlem.Recruitment.Campaigns.Index.Query());
            campaigns.Tap(value => Campaigns = value);

            Trainings = await _engine.Query(new Szlem.Recruitment.Trainings.Index.Query());

            return Page();
        }

        public async Task<IActionResult> OnPostExportToCsv(GetSubmissions.Query query)
        {
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            var submissions = await _engine.Query(query);

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.StreamWriter(stream))
            using (var csv = new CsvHelper.CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentUICulture)))
            {
                csv.WriteRecords(submissions.Select(x => new {
                    Timestamp = x.Timestamp.ToString(),
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    FullName = x.FullName,
                    EmailAddress = x.Email.ToString(),
                    PhoneNumber = x.PhoneNumber.ToString(),
                    Region = x.Region,
                    HasLecturerRights = x.HasLecturerRights,
                    HasResigned = x.HasResignedPermanently || x.HasResignedTemporarily
                }));
                csv.Flush();
                writer.Flush();
                return File(stream.ToArray(), "text/csv", "enrollments.csv");
            }
        }
    }
}
