using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Phases",
                columns: table => new
                {
                    PhaseId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phases", x => x.PhaseId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TableAffected = table.Column<string>(type: "TEXT", nullable: false),
                    RecordId = table.Column<int>(type: "INTEGER", nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "ClearanceRequests",
                columns: table => new
                {
                    ClearanceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClearanceType = table.Column<string>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearanceRequests", x => x.ClearanceId);
                });

            migrationBuilder.CreateTable(
                name: "DuesRecords",
                columns: table => new
                {
                    DuesId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DueDate = table.Column<string>(type: "TEXT", nullable: false),
                    PaidDate = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuesRecords", x => x.DuesId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EventDate = table.Column<string>(type: "TEXT", nullable: false),
                    Venue = table.Column<string>(type: "TEXT", nullable: true),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "HOASettings",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                        .Annotation("Sqlite:Autoincrement", true),
                    HOAName = table.Column<string>(type: "TEXT", nullable: false),
                    Subdivision = table.Column<string>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    Province = table.Column<string>(type: "TEXT", nullable: false),
                    PresidentName = table.Column<string>(type: "TEXT", nullable: true),
                    SecretaryName = table.Column<string>(type: "TEXT", nullable: true),
                    TreasurerName = table.Column<string>(type: "TEXT", nullable: true),
                    ContactNumber = table.Column<string>(type: "TEXT", nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOASettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Homeowners",
                columns: table => new
                {
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    MiddleName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    BirthDate = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    CivilStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ContactNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    PhaseId = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Categories = table.Column<string>(type: "TEXT", nullable: true),
                    ResidencySince = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Homeowners", x => x.HomeownerId);
                    table.ForeignKey(
                        name: "FK_Homeowners_Phases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "Phases",
                        principalColumn: "PhaseId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Homeowners_HomeownerId",
                        column: x => x.HomeownerId,
                        principalTable: "Homeowners",
                        principalColumn: "HomeownerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    PhaseId = table.Column<int>(type: "INTEGER", nullable: true),
                    HeadHomeownerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                    table.ForeignKey(
                        name: "FK_Units_Homeowners_HeadHomeownerId",
                        column: x => x.HeadHomeownerId,
                        principalTable: "Homeowners",
                        principalColumn: "HomeownerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Units_Phases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "Phases",
                        principalColumn: "PhaseId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Units_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViolationRecords",
                columns: table => new
                {
                    ViolationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ViolationNumber = table.Column<string>(type: "TEXT", nullable: false),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: true),
                    HomeownerName = table.Column<string>(type: "TEXT", nullable: false),
                    ViolationType = table.Column<string>(type: "TEXT", nullable: false),
                    ViolationDate = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Resolution = table.Column<string>(type: "TEXT", nullable: true),
                    FiledAt = table.Column<string>(type: "TEXT", nullable: false),
                    FiledBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViolationRecords", x => x.ViolationId);
                    table.ForeignKey(
                        name: "FK_ViolationRecords_Homeowners_HomeownerId",
                        column: x => x.HomeownerId,
                        principalTable: "Homeowners",
                        principalColumn: "HomeownerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ViolationRecords_Users_FiledBy",
                        column: x => x.FiledBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViolationRecords_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MSMEs",
                columns: table => new
                {
                    MSMEId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessName = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessType = table.Column<string>(type: "TEXT", nullable: false),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContactNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RegistrationDate = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MSMEs", x => x.MSMEId);
                    table.ForeignKey(
                        name: "FK_MSMEs_Homeowners_HomeownerId",
                        column: x => x.HomeownerId,
                        principalTable: "Homeowners",
                        principalColumn: "HomeownerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MSMEs_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MSMEs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InteractionLogs",
                columns: table => new
                {
                    InteractionLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HomeownerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MSMEId = table.Column<int>(type: "INTEGER", nullable: true),
                    InteractionType = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    InteractionDate = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractionLogs", x => x.InteractionLogId);
                    table.ForeignKey(
                        name: "FK_InteractionLogs_Homeowners_HomeownerId",
                        column: x => x.HomeownerId,
                        principalTable: "Homeowners",
                        principalColumn: "HomeownerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InteractionLogs_MSMEs_MSMEId",
                        column: x => x.MSMEId,
                        principalTable: "MSMEs",
                        principalColumn: "MSMEId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InteractionLogs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EventId_HomeownerId",
                table: "Attendances",
                columns: new[] { "EventId", "HomeownerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_HomeownerId",
                table: "Attendances",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_RecordedBy",
                table: "Attendances",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceRequests_HomeownerId",
                table: "ClearanceRequests",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceRequests_ProcessedBy",
                table: "ClearanceRequests",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DuesRecords_CreatedBy",
                table: "DuesRecords",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DuesRecords_HomeownerId_Month_Year",
                table: "DuesRecords",
                columns: new[] { "HomeownerId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedBy",
                table: "Events",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HOASettings_UpdatedBy",
                table: "HOASettings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Homeowners_CreatedBy",
                table: "Homeowners",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Homeowners_PhaseId",
                table: "Homeowners",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Homeowners_UnitId",
                table: "Homeowners",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionLogs_CreatedBy",
                table: "InteractionLogs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionLogs_HomeownerId",
                table: "InteractionLogs",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionLogs_MSMEId",
                table: "InteractionLogs",
                column: "MSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_MSMEs_CreatedBy",
                table: "MSMEs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MSMEs_HomeownerId",
                table: "MSMEs",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_MSMEs_UnitId",
                table: "MSMEs",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_CreatedBy",
                table: "Units",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Units_HeadHomeownerId",
                table: "Units",
                column: "HeadHomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_PhaseId",
                table: "Units",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_HomeownerId",
                table: "Users",
                column: "HomeownerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViolationRecords_FiledBy",
                table: "ViolationRecords",
                column: "FiledBy");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationRecords_HomeownerId",
                table: "ViolationRecords",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationRecords_UpdatedBy",
                table: "ViolationRecords",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationRecords_ViolationNumber",
                table: "ViolationRecords",
                column: "ViolationNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Events_EventId",
                table: "Attendances",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Homeowners_HomeownerId",
                table: "Attendances",
                column: "HomeownerId",
                principalTable: "Homeowners",
                principalColumn: "HomeownerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_RecordedBy",
                table: "Attendances",
                column: "RecordedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClearanceRequests_Homeowners_HomeownerId",
                table: "ClearanceRequests",
                column: "HomeownerId",
                principalTable: "Homeowners",
                principalColumn: "HomeownerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClearanceRequests_Users_ProcessedBy",
                table: "ClearanceRequests",
                column: "ProcessedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DuesRecords_Homeowners_HomeownerId",
                table: "DuesRecords",
                column: "HomeownerId",
                principalTable: "Homeowners",
                principalColumn: "HomeownerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DuesRecords_Users_CreatedBy",
                table: "DuesRecords",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Users_CreatedBy",
                table: "Events",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HOASettings_Users_UpdatedBy",
                table: "HOASettings",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Homeowners_Units_UnitId",
                table: "Homeowners",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Homeowners_Users_CreatedBy",
                table: "Homeowners",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Homeowners_HeadHomeownerId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Homeowners_HomeownerId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ClearanceRequests");

            migrationBuilder.DropTable(
                name: "DuesRecords");

            migrationBuilder.DropTable(
                name: "HOASettings");

            migrationBuilder.DropTable(
                name: "InteractionLogs");

            migrationBuilder.DropTable(
                name: "ViolationRecords");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "MSMEs");

            migrationBuilder.DropTable(
                name: "Homeowners");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Phases");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
