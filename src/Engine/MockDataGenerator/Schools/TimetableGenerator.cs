using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Engine.Stakeholders.RegionalCoordinator.Schools;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.GetTimetableUseCase;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.Timetable;

namespace MockDataGenerator.Schools
{
    public class TimetableGenerator
    {
        public Timetable GetTimetable()
        {
            var random = new Random();

            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            var timeSlots = Enumerable.Range(1, 8).Select(x => GetLessonTime(x)).ToArray();
            
            var lessons = new Dictionary<DayOfWeek, DayTimetable>();
            for (int i = 1; i <= 8; ++i)
            {
                var dayOfWeek = days[random.Next(0, days.Length)];
                var timeSlot = timeSlots[random.Next(0, timeSlots.Length)];
                var lessonCount = random.Next(2 + 1);
                lessons[dayOfWeek][random.Next(0, timeSlots.Length)] = Enumerable.Range(0, lessonCount).Select(x => "1" + (char)(random.Next(8) + 'a')).ToArray();
            }

            var timetable = new Timetable(timeSlots, lessons, DateTime.Today, 0, 0);

            return timetable;
        }


        private LessonTimeSlot GetLessonTime(int lessonNo)
        {
            (TimeSpan startTime, TimeSpan endTime) = GetLessonTimeTuple(lessonNo);
            return new LessonTimeSlot(startTime, endTime );
        }

        private (TimeSpan startTime, TimeSpan endTime) GetLessonTimeTuple(int lessonNo)
        {
            switch (lessonNo)
            {
                case 0:
                    return (new TimeSpan(7, 15, 0), new TimeSpan(8, 0, 0));
                case 1:
                    return (new TimeSpan(8, 10, 0), new TimeSpan(8, 55, 0));
                case 2:
                    return (TimeSpan.Parse("9:05"), TimeSpan.Parse("9:50"));
                case 3:
                    return (TimeSpan.Parse("10:00"), TimeSpan.Parse("10:45"));
                case 4:
                    return (TimeSpan.Parse("11:05"), TimeSpan.Parse("11:50"));
                case 5:
                    return (TimeSpan.Parse("12:00"), TimeSpan.Parse("12:45"));
                case 6:
                    return (TimeSpan.Parse("12:55"), TimeSpan.Parse("13:40"));
                case 7:
                    return (TimeSpan.Parse("13:50"), TimeSpan.Parse("14:35"));
                case 8:
                    return (TimeSpan.Parse("14:45"), TimeSpan.Parse("15:30"));
                default:
                    throw new ArgumentException(nameof(lessonNo));
            }
        }
    }
}
