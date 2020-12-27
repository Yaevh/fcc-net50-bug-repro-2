using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Recruitment.Impl.Entities.Mappings;

namespace Szlem.Recruitment.Impl.Entities.Migrations
{
    [Migration(2020_04_23_02_15, "adding Id to TrainingNote_wo_make_it_Entity")]
    public class _2020_04_23_TrainingNote_as_Entity : Migration
    {
        public override void Up()
        {
            Delete.Table(TrainingMapping.NoteMapping.Notes_TableName);

            Create.Table(TrainingMapping.NoteMapping.Notes_TableName)
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_Id).AsInt32().PrimaryKey()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_AuthorId).AsGuid().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_Content).AsString().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_Timestamp).AsString().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_TrainingId).AsInt32()
                    .ForeignKey(TrainingMapping.TableName, TrainingMapping.Column_Id);
        }

        public override void Down()
        {
            
        }
    }
}
