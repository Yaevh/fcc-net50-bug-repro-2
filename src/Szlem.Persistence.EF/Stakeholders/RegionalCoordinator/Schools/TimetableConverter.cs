using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EngineTimetable = Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.Timetable;
using ModelTimetable = Szlem.Models.Schools.Timetable;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    internal class TimetableConverter
    {
        public EngineTimetable Convert(ModelTimetable timetable)
        {
            return new EngineTimetable(
                timetable.TimeSlots.Select(Convert).ToList(),
                timetable.Lessons.ToDictionary(x => x.Key, x => Convert(timetable.Lessons[x.Key])),
                timetable.ValidFrom,
                timetable.Id,
                timetable.School?.ID
            );
        }

        private EngineTimetable.LessonTimeSlot Convert(ModelTimetable.LessonTimeSlot timeSlot)
        {
            return new EngineTimetable.LessonTimeSlot(timeSlot.StartTime, timeSlot.EndTime);
        }

        private EngineTimetable.DayTimetable Convert(ModelTimetable.DayTimetable dayTimetable)
        {
            var result = new EngineTimetable.DayTimetable();
            foreach (var day in dayTimetable.Keys)
                result[day] = dayTimetable[day];
            return result;
        }


        public ModelTimetable Convert(EngineTimetable timetable)
        {
            return new ModelTimetable(
                timetable.TimeSlots.Select(Convert).ToList(),
                timetable.Lessons.ToDictionary(x => x.Key, x => Convert(timetable.Lessons[x.Key])),
                timetable.ValidFrom,
                timetable.ID
            );
        }

        private ModelTimetable.LessonTimeSlot Convert(EngineTimetable.LessonTimeSlot source)
        {
            return new ModelTimetable.LessonTimeSlot() { StartTime = source.StartTime, EndTime = source.EndTime };
        }

        private ModelTimetable.DayTimetable Convert(EngineTimetable.DayTimetable source)
        {
            var result = new ModelTimetable.DayTimetable();
            foreach (var day in source.Keys)
                result[day] = source[day];
            return result;
        }
    }
}
