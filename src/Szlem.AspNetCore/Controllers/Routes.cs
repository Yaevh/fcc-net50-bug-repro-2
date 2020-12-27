using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore
{
    public class Routes
    {
        public const string Root = "api";

        public static class v1
        {
            public const string Version = "/v1";

            public static class Editions
            {
                public const string Controller = "/editions";

                public const string Index = Root + Version + Controller;
                public const string Details = Root + Version + Controller + "/{id}";
                public const string Create = Root + Version + Controller;

                public static string DetailsFor(int id) => Details.Replace("{id}", id.ToString());
            }

            public static class RecruitmentCampaigns
            {
                public const string Controller = Editions.Controller + "/recruitment";

                public const string Index = Root + Version + Controller;
                public const string Details = Root + Version + Controller + "/{id}";
                public static string DetailsFor(int id) => Details.Replace("{id}", id.ToString());
                public const string Create = Root + Version + Controller;
                public const string ScheduleTraining = Root + Version + Controller + "/schedule-training";
            }

            public static class Enrollments
            {
                public const string Controller = "/enrollments";
                public const string GetEnrollment = Root + Version + Controller + "/{enrollmentID}";
                public const string GetSubmissions = Root + Version + Controller + "/submissions";
                public const string SubmitRecruitmentForm = Root + Version + Controller + "/submit";
                public const string RecordAcceptedTrainingInvitation = Root + Version + Controller + "/record-accepted-training-invitation";
                public const string RecordRefusedTrainingInvitation = Root + Version + Controller + "/record-refused-training-invitation";
                public const string RecordTrainingResults = Root + Version + Controller + "/record-training-results";
                public const string RecordResignation = Root + Version + Controller + "/record-resignation";
                public const string RecordContact = Root + Version + Controller + "/record-contact";
            }

            public static class Identity
            {
                public const string Controller = "/identity";

                public const string Login = Root + Version + Controller + "/login";
                public const string Register = Root + Version + Controller + "/register";
            }

            public static class Schools
            {
                public const string Controller = "/schools";
                public const string Index = Root + Version + Controller;
                public const string Details = Root + Version + Controller + "/{schoolId}";
                public const string Register = Root + Version + Controller + "/register";
                public const string RecordInitialAgreement = Root + Version + Controller + "/record-initial-agreement";
                public const string RecordAgreementSigned = Root + Version + Controller + "/record-agreement-signed";
                public const string RecordResignation = Root + Version + Controller + "/record-resignation";
                public const string RecordContact = Root + Version + Controller + "/record-contact";

                public const string AddNote = Root + Version + Controller + "/notes/add";
                public const string EditNote = Root + Version + Controller + "/notes/edit";
                public const string DeleteNote = Root + Version + Controller + "/notes/delete";

                public static string DetailsFor(Guid schoolId) => Details.Replace("{schoolId}", schoolId.ToString());
            }
        }
    }
}
