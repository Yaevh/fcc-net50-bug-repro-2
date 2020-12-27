using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Szlem.Domain.Exceptions;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public class Timetable
    {
        public int ID { get; set; }

        public int SchoolID { get; set; }

        public DayOfWeek[] Days { get; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        public IReadOnlyList<LessonTimeSlot> TimeSlots { get; }

        public IDictionary<DayOfWeek, DayTimetable> Lessons { get; }

        
        [Display(Name = "Obowiązuje od")]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; } = DateTime.Today;


        public Timetable(IReadOnlyList<LessonTimeSlot> timeSlots, IDictionary<DayOfWeek, DayTimetable> lessons, DateTime validFrom, int id, int? schoolID)
        {
            TimeSlots = timeSlots ?? throw new ArgumentNullException(nameof(timeSlots));
            Lessons = lessons ?? throw new ArgumentNullException(nameof(lessons));

            if (validFrom == default(DateTime))
                throw new ArgumentNullException(nameof(validFrom));
            ValidFrom = validFrom;
            SchoolID = schoolID ?? default(int);

            ID = id;
        }


        public class DayTimetable : Dictionary<int, string[]> { }

        public class LessonTimeSlot : IEquatable<LessonTimeSlot>
        {
            public TimeSpan StartTime { get; }

            public TimeSpan EndTime { get; }


            public LessonTimeSlot(TimeSpan startTime, TimeSpan endTime)
            {
                if (startTime == default(TimeSpan))
                    throw new ArgumentNullException(nameof(startTime));
                if (endTime == default(TimeSpan))
                    throw new ArgumentNullException(nameof(endTime));
                if (startTime >= endTime)
                    throw new SzlemException($"{nameof(startTime)} must be earlier than {nameof(endTime)}");

                StartTime = startTime;
                EndTime = endTime;
            }


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
                return new LessonTimeSlot(startTime, endTime);
            }

            public static implicit operator string(LessonTimeSlot timeSlot) => timeSlot.ToString();

            public static implicit operator LessonTimeSlot(string value) => LessonTimeSlot.Parse(value);

            #endregion

        }


        public class TimetableValidator : AbstractValidator<Timetable>
        {
            private const string TimetableMustContainAlDaysOfWeek = "Timetable must contain all days of the week";
            private const string TimetableCannotContainDuplicateDays = "Timetable cannot contain duplicate days";
            private const string TimetableCannotContainDuplicateLessonStartTimes = "Timetable cannot contain duplicate lesson start times";
            private const string TimetableCannotContainDuplicateLessonEndTimes = "Timetable cannot contain duplicate lesson end times";
            private const string TimetableLessonTimesMustBeInAscendingOrder = "Timetable lesson times must be in ascending order";
            private const string TimetableLessonTimesCannotOverlap = "Timetable lesson times cannot overlap";
            private const string LessonsDayAndTimeMustMatchItsPositionInTimetable = "Lesson's day and time must match its position in timetable";

            public TimetableValidator()
            {
                RuleFor(x => x.Days).Must(x => x.Length == 7).WithMessage(TimetableMustContainAlDaysOfWeek);
                RuleFor(x => x.Days).Must(x => x.HasDuplicates() == false).WithMessage(TimetableCannotContainDuplicateDays);

                RuleFor(x => x.TimeSlots)
                    .Must(x => x.Select(y => y.StartTime).HasDuplicates() == false)
                    .WithMessage(TimetableCannotContainDuplicateLessonStartTimes);
                RuleFor(x => x.TimeSlots)
                    .Must(x => x.Select(y => y.EndTime).HasDuplicates() == false)
                    .WithMessage(TimetableCannotContainDuplicateLessonEndTimes);
                RuleFor(x => x.TimeSlots)
                    .Must(x => x.IsOrderedBy(y => y.StartTime) && x.IsOrderedBy(y => y.EndTime))
                    .WithMessage(TimetableLessonTimesMustBeInAscendingOrder);
                RuleForEach(x => x.TimeSlots).SetValidator(new LessonTimeSlotValidator());
                RuleFor(x => x.TimeSlots)
                    .Must(timeSlots => {
                        for (int i = 1; i < timeSlots.Count; ++i)
                            if (timeSlots[i - 1].EndTime > timeSlots[i].StartTime)
                                return false;
                        return true;
                    })
                    .WithMessage(TimetableLessonTimesCannotOverlap);

                RuleFor(x => x.ValidFrom).NotEmpty();
            }
        }

        public class LessonTimeSlotValidator : AbstractValidator<LessonTimeSlot>
        {
            private const string StartTimeCannotBeEmpty = "Start time cannot be empty";
            private const string EndTimeCannotBeEmpty = "End time cannot be empty";
            private const string StartTimeMustBeEarlierThanEndTime = "Start time must be earlier than end time";
            private const string EndTimeMustBeLaterThanStartTime = "End time must be later than start time";

            public LessonTimeSlotValidator()
            {
                RuleFor(x => x.StartTime).NotEmpty().WithMessage(StartTimeCannotBeEmpty);
                RuleFor(x => x.EndTime).NotEmpty().WithMessage(EndTimeCannotBeEmpty);
                RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage(StartTimeMustBeEarlierThanEndTime);
                RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage(EndTimeMustBeLaterThanStartTime);
            }
        }
    }
    
}
