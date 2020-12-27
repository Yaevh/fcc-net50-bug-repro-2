using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Szlem.Persistence.EF.Migrations
{
    public partial class UserIdAsGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTraining");

            migrationBuilder.DropTable(
                name: "RecruitmentCampaign");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecruitmentCampaign",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EditionID = table.Column<int>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCampaign", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Edition_EditionID",
                        column: x => x.EditionID,
                        principalTable: "Edition",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTraining",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(nullable: true),
                    CampaignID = table.Column<int>(nullable: false),
                    City = table.Column<string>(nullable: true),
                    CoordinatorID = table.Column<string>(nullable: true),
                    EndDateTime = table.Column<DateTime>(nullable: false),
                    StartDateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTraining", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ScheduledTraining_RecruitmentCampaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_EditionID",
                table: "RecruitmentCampaign",
                column: "EditionID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTraining_CampaignID",
                table: "ScheduledTraining",
                column: "CampaignID");
        }
    }
}
