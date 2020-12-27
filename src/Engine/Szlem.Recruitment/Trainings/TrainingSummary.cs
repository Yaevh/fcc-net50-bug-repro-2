using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Trainings
{
    public enum TrainingTiming
    {
        Past,
        Current,
        Future
    }


    public class TrainingSummary
    {
        public int ID { get; set; }

        public NodaTime.OffsetDateTime StartDateTime { get; set; }
        public NodaTime.OffsetDateTime EndDateTime { get; set; }

        public Guid CoordinatorID { get; set; }
        public string CoordinatorName { get; set; }

        public string City { get; set; }
        public string Address { get; set; }

        public TrainingTiming Timing { get; set; }

        public override string ToString()
        {
            return $"{Address}, {City}, {StartDateTime.ToString("uuuu'-'MM'-'dd' 'HH':'mm", null)}-{EndDateTime.ToString("HH':'mm o<g>", null)}, {CoordinatorName}";
        }
    }
}
