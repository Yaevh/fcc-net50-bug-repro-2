using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Szlem.Domain;
using Szlem.Models;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Lecturers
{
    public static class Index
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<IPagedList<LecturerSummary>>
        {
            public int PageSize { get; set; } = 25;

            public int PageNo { get; set; } = 1;
        }

        public class LecturerSummary
        {
            public string Name { get; set; }

            public EmailAddress Email { get; set; }

            public PhoneNumber PhoneNumber { get; set; }

            public bool CanShowDetails { get; set; }
        }
    }
}
