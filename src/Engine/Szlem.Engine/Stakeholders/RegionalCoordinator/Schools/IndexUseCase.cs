using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class IndexUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<IReadOnlyCollection<SchoolSummary>> { }

        public class SchoolSummary
        {
            public int ID { get; set; }

            public string Name { get; set; }

            public string Address { get; set; }

            public string City { get; set; }
        }
    }
}
