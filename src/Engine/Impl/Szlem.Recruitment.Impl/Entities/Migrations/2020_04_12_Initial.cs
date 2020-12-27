using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Recruitment.Impl.Entities.Mappings;

namespace Szlem.Recruitment.Impl.Entities.Migrations
{
    [Migration(2020_04_12, "Initial migration")]
    public class _2020_04_12_Initial : Migration
    {
        public override void Up()
        {
            /* create table "Campaign" (
                ID  integer primary key autoincrement,
                Name TEXT,
                StartDateTime TEXT,
                EndDateTime TEXT,
                EditionId INT
            )*/
            Create.Table(CampaignMapping.TableName)
                .WithColumn(CampaignMapping.Columns_Id).AsInt32().PrimaryKey()
                .WithColumn(CampaignMapping.Columns_Name).AsString().Nullable()
                .WithColumn(CampaignMapping.Columns_StartDateTime).AsString().NotNullable()
                .WithColumn(CampaignMapping.Columns_EndDateTime).AsString().NotNullable()
                .WithColumn(CampaignMapping.Columns_EditionId).AsInt32().Indexed().NotNullable();

            /* create table Training (
                ID  integer primary key autoincrement,
                StartDateTime TEXT,
                EndDateTime TEXT,
                Address TEXT,
                City TEXT,
                CoordinatorID TEXT,
                CampaignID INT,
                constraint FK_EC125F09 foreign key (CampaignID) references "Campaign"
            )*/
            Create.Table(TrainingMapping.TableName)
                .WithColumn(TrainingMapping.Column_Id).AsInt32().PrimaryKey()
                .WithColumn(TrainingMapping.Column_StartDateTime).AsString().NotNullable()
                .WithColumn(TrainingMapping.Column_EndDateTime).AsString().NotNullable()
                .WithColumn(TrainingMapping.Column_Address).AsString().NotNullable()
                .WithColumn(TrainingMapping.Column_City).AsString().NotNullable()
                .WithColumn(TrainingMapping.Column_CoordinatorId).AsGuid().NotNullable()
                .WithColumn(TrainingMapping.Column_CampaignId).AsInt32().NotNullable()
                    .ForeignKey(CampaignMapping.TableName, CampaignMapping.Columns_Id);

            /* create table Notes (
                TrainingID INT not null,
                AuthorId UNIQUEIDENTIFIER,
                Content TEXT,
                Timestamp TEXT,
                constraint FK_C8260241 foreign key (TrainingID) references Training
            )*/
            Create.Table(TrainingMapping.NoteMapping.Notes_TableName)
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_AuthorId).AsGuid().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_Content).AsString().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_Timestamp).AsString().NotNullable()
                .WithColumn(TrainingMapping.NoteMapping.Notes_Column_TrainingId).AsInt32()
                    .ForeignKey(TrainingMapping.TableName, TrainingMapping.Column_Id);
        }

        public override void Down()
        {
            Delete.Table(CampaignMapping.TableName);
            Delete.Table(TrainingMapping.TableName);
            Delete.Table(TrainingMapping.NoteMapping.Notes_TableName);
        }
    }
}
