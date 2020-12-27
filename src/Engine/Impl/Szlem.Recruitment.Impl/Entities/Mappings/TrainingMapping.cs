using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Entities.Mappings
{
    internal class TrainingMapping : ClassMap<Training>
    {
        public const string TableName = "Trainings";
        public const string Column_Id = "Id";
        public const string Column_StartDateTime = "StartDateTime";
        public const string Column_EndDateTime = "EndDateTime";
        public const string Column_Address = "Address";
        public const string Column_City = "City";
        public const string Column_CoordinatorId = "CoordinatorId";
        public const string Column_CampaignId = "CampaignId";

        public TrainingMapping()
        {
            Table(TableName);
            Id(m => m.ID, Column_Id);
            Map(m => m.StartDateTime, Column_StartDateTime);
            Map(m => m.EndDateTime, Column_EndDateTime);
            Map(m => m.Address, Column_Address);
            Map(m => m.City, Column_City);
            Map(m => m.CoordinatorID, Column_CoordinatorId).CustomType<StringBackedGuidType>();

            HasMany(m => m.Notes)
                .Access.CamelCaseField(Prefix.Underscore)
                .Cascade.AllDeleteOrphan();

            References(m => m.Campaign, Column_CampaignId);
        }

        internal class NoteMapping : ClassMap<Training.Note>
        {
            public const string Notes_TableName = "TrainingNotes";
            public const string Notes_Column_Id = "Id";
            public const string Notes_Column_TrainingId = "TrainingId";
            public const string Notes_Column_AuthorId = "AuthorId";
            public const string Notes_Column_Content = "Content";
            public const string Notes_Column_Timestamp = "Timestamp";

            public NoteMapping()
            {
                Table(Notes_TableName);
                Id(x => x.Id, Notes_Column_Id);
                Map(x => x.AuthorId, Notes_Column_AuthorId);
                Map(x => x.Content, Notes_Column_Content);
                Map(x => x.Timestamp, Notes_Column_Timestamp);
                References(x => x.Training, Notes_Column_TrainingId);
            }
        }
    }
}
