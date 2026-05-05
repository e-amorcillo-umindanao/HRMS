using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairAttendanceSubdivisionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubdivisionId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE attendance
                SET attendance.SubdivisionId = eventRecord.SubdivisionId
                FROM Attendances AS attendance
                INNER JOIN Events AS eventRecord ON attendance.EventId = eventRecord.EventId
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SubdivisionId",
                table: "Attendances",
                column: "SubdivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Subdivisions_SubdivisionId",
                table: "Attendances",
                column: "SubdivisionId",
                principalTable: "Subdivisions",
                principalColumn: "SubdivisionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Subdivisions_SubdivisionId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SubdivisionId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SubdivisionId",
                table: "Attendances");
        }
    }
}
