using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import
{
    public class RequestFromCsv : IRequest
    {
        public int RecruitmentCampaignID { get; set; }

        public Stream TextContent { get; set; }
    }
}
