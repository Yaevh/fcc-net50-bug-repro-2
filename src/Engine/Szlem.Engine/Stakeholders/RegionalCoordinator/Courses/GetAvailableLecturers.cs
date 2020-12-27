using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Courses
{
    public static class GetAvailableLecturers
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Response>
        {

        }

        public class Response : IReadOnlyCollection<AvailableLecturer>
        {
            private readonly List<AvailableLecturer> _availableLecturers;

            public Response(IEnumerable<AvailableLecturer> availableLecturers)
            {
                _availableLecturers = availableLecturers?.ToList() ?? throw new ArgumentNullException(nameof(availableLecturers));
            }

            public int Count => _availableLecturers.Count;

            public IEnumerator<AvailableLecturer> GetEnumerator() => ((IReadOnlyCollection<AvailableLecturer>)_availableLecturers).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class AvailableLecturer
        {
            public int ID { get; }
            public string Name { get; }
            public IReadOnlyCollection<string> Cities { get; }

            public AvailableLecturer(int id, string name, IEnumerable<string> cities)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Lecturer name cannot be empty", nameof(name));
                if (cities == null)
                    throw new ArgumentNullException(nameof(cities));

                ID = id;
                Name = name;
                Cities = cities.ToArray();
            }
        }
    }
}
