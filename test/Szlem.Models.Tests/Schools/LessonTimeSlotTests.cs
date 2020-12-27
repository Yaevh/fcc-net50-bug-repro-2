using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Schools;
using Xunit;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.GetTimetableUseCase;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.Timetable;

namespace Szlem.Models.Tests.Schools
{
    public class LessonTimeSlotTests
    {
        [Fact]
        public void ShouldSerializeToProperString()
        {
            var timeSlot = new LessonTimeSlot(new TimeSpan(8, 0, 0), new TimeSpan(8, 45, 0));
            string asString = timeSlot;
            Assert.Equal("8:00 - 8:45", asString);
        }

        [Theory]
        [InlineData("8:00 - 8:45")]
        [InlineData("08:00 - 08:45")]
        [InlineData("8:00-8:45")]
        [InlineData("8:00 -8:45")]
        [InlineData("8:00- 8:45")]
        [InlineData("8:00    -   8:45")]
        public void ParsingCorrectString_ShouldSucceed(string value)
        {
            LessonTimeSlot timeSlot = value;
        }

        [Theory]
        [InlineData("008:00 - 8:45")]
        [InlineData("08:00 - 08:45:30")]
        public void ParsingInvalidString_ShouldThrowFormatException(string value)
        {
            Assert.Throws<FormatException>(() => LessonTimeSlot.Parse(value));
        }
    }
}
