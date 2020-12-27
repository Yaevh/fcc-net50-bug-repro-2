using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Entities.Mappings
{
    internal class CampaignMapping : ClassMap<Campaign>
    {
        public const string TableName = "Campaigns";
        public const string Columns_Id = "Id";
        public const string Columns_Name = "Name";
        public const string Columns_StartDateTime = "StartDateTime";
        public const string Columns_EndDateTime = "EndDateTime";
        public const string Columns_EditionId = "EditionId";

        public CampaignMapping()
        {
            Table(TableName);
            Id(x => x.Id);
            Map(x => x.Name, Columns_Name);
            Map(x => x.StartDateTime, Columns_StartDateTime);
            Map(x => x.EndDateTime, Columns_EndDateTime);
            Map(x => x.EditionId, Columns_EditionId);
            HasMany(x => x.Trainings).AsSet().Access.CamelCaseField(Prefix.Underscore).Cascade.AllDeleteOrphan();
        }
    }
}
