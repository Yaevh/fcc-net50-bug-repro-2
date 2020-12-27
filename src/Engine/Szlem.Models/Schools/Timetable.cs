using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Szlem.Models.Schools
{
    public class Timetable
    {
        public int Id { get; set; }

        public School School { get; set; }


        public DayOfWeek[] Days { get; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        public IList<LessonTimeSlot> TimeSlots { get; private set; } = new List<LessonTimeSlot>();

        public IDictionary<DayOfWeek, DayTimetable> Lessons { get; }

        [Display(Name = "Obowiązuje od")]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; } = DateTime.Today;


        public Timetable(IList<LessonTimeSlot> timeSlots, IDictionary<DayOfWeek, DayTimetable> lessons, DateTime validFrom, int id)
        {
            TimeSlots = timeSlots ?? throw new ArgumentNullException(nameof(timeSlots));
            if (lessons == null)
                lessons = new Dictionary<DayOfWeek, DayTimetable>();
            Lessons = Days.ToDictionary(x => x, x => lessons.ContainsKey(x) ? lessons[x] : new DayTimetable());
            Lessons = lessons ?? throw new ArgumentNullException(nameof(lessons));
            if (validFrom == default(DateTime))
                throw new ArgumentNullException(nameof(validFrom));
            ValidFrom = validFrom;

            Id = id;
        }

        
        public class DayTimetable : Dictionary<int, string[]> { }

        public class LessonTimeSlot : IEquatable<LessonTimeSlot>
        {
            public TimeSpan StartTime { get; set; }

            public TimeSpan EndTime { get; set; }


            #region equality and hash code

            public override bool Equals(object obj)
            {
                return Equals(obj as LessonTimeSlot);
            }

            public bool Equals(LessonTimeSlot other)
            {
                return other != null &&
                       StartTime.Equals(other.StartTime) &&
                       EndTime.Equals(other.EndTime);
            }

            public override int GetHashCode()
            {
                int hash = 37;
                hash = hash * 41 + StartTime.GetHashCode();
                hash = hash * 41 + EndTime.GetHashCode();
                return hash;
            }

            public static bool operator ==(LessonTimeSlot slot1, LessonTimeSlot slot2)
            {
                return EqualityComparer<LessonTimeSlot>.Default.Equals(slot1, slot2);
            }

            public static bool operator !=(LessonTimeSlot slot1, LessonTimeSlot slot2)
            {
                return !(slot1 == slot2);
            }

            #endregion

            #region converting to and from string

            private const string RegexPattern = @"^(?<startTime>\d{1,2}:\d{2})\s*-\s*(?<endTime>\d{1,2}:\d{2})$";


            public override string ToString() => $"{StartTime.ToString("h':'mm")} - {EndTime.ToString("h':'mm")}";

            public static LessonTimeSlot Parse(string value)
            {
                var regex = new Regex(RegexPattern);
                var match = regex.Match(value);
                if (match.Success == false)
                    throw new FormatException("input string must be in format 'HH:mm - HH:mm'");
                var startTime = TimeSpan.Parse(match.Groups["startTime"].Value, System.Globalization.CultureInfo.InvariantCulture);
                var endTime = TimeSpan.Parse(match.Groups["endTime"].Value, System.Globalization.CultureInfo.InvariantCulture);
                return new LessonTimeSlot() { StartTime = startTime, EndTime = endTime };
            }

            public static implicit operator string(LessonTimeSlot timeSlot) => timeSlot.ToString();

            public static implicit operator LessonTimeSlot(string value) => Parse(value);

            #endregion

        }
        
    }
}
