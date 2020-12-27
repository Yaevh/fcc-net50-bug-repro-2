using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Szlem.Models.Schools.Timetable;

namespace Szlem.Models.Schools.Validators
{
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
