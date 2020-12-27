using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Areas.Recruitment
{
    public class Consts
    {
        public const string AreaName = "Recruitment";
        public const string EnrollmentsSubAreaName = "Enrollments";
        public const string TrainigsSubAreaName = "Trainings";

        public static string EnrollmentPageRoute(string pageName) => $"/{EnrollmentsSubAreaName}/{pageName}";
        public static string TrainingPageRoute(string pageName) => $"/{TrainigsSubAreaName}/{pageName}";
    }
}
