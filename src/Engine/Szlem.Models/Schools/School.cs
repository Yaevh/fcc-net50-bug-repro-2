using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Szlem.Domain;

namespace Szlem.Models.Schools
{
    [Obsolete]
    public class School
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public string Website { get; set; }

        public PhoneNumber ContactPhoneNumber { get; set; }

        public string Email { get; set; }


        public virtual IReadOnlyList<ContactPerson> ContactPersons => _contactPersons;
        private List<ContactPerson> _contactPersons = new List<ContactPerson>();

        public void AddContactPerson(ContactPerson person) => _contactPersons.Add(person);

        public bool RemoveContactPerson(ContactPerson person) => _contactPersons.Remove(person);


        #region Timetables

        public virtual IReadOnlyCollection<Timetable> Timetables => _timetables;
        private List<Timetable> _timetables = new List<Timetable>();

        public void AddTimetable(Timetable timetable)
        {
            if (timetable == null)
                throw new ArgumentNullException(nameof(timetable));
            timetable.School = this;
            _timetables.Add(timetable);
        }

        public bool RemoveTimetable(Timetable timetable)
        {
            if (timetable == null)
                throw new ArgumentNullException(nameof(timetable));
            var result = _timetables.Remove(timetable);
            if (result)
                timetable.School = null;
            return result;
        }

        #endregion
    }
}
