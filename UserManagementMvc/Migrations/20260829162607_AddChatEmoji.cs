using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace UserManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEmoji : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChatEmojis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Emoji = table.Column<string>(type: "longtext", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatEmojis", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "securityquestionmasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    QuestionText = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    CreatedByRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sitecontents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ContentKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sitecontentsections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PageName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    SectionTitle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    SectionContent = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true, defaultValueSql: "'1'"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    PasswordHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, defaultValueSql: "'User'"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    UserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SecurityQuestion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    SecurityAnswerHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, defaultValueSql: "'General'"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    MobileCountryCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, defaultValueSql: "'+91'"),
                    DateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UnblockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UnblockedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "auditlogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chatmessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeliveredAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    SeenAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDeletedBySender = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsDeletedByRecipient = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AttachmentUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_RecipientUser",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_SenderUser",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "emailotps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    OtpHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailOtps_Users",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mailmessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    SenderEmail = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    RecipientUserId = table.Column<int>(type: "int", nullable: true),
                    RecipientEmail = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsStarred = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsDraft = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DraftSavedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDeletedBySender = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsDeletedByRecipient = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsPermanentlyDeletedBySender = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsPermanentlyDeletedByRecipient = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReadAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    MessageType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "Internal"),
                    ParentMessageId = table.Column<int>(type: "int", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailMessages_ParentMessage",
                        column: x => x.ParentMessageId,
                        principalTable: "mailmessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MailMessages_RecipientUser",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MailMessages_SenderUser",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usersecurityquestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SecurityQuestionMasterId = table.Column<int>(type: "int", nullable: true),
                    QuestionText = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    SecurityAnswerHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSecurityQuestions_SecurityQuestionMasters",
                        column: x => x.SecurityQuestionMasterId,
                        principalTable: "securityquestionmasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserSecurityQuestions_Users",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "auditlogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Conversation",
                table: "chatmessages",
                columns: new[] { "SenderUserId", "RecipientUserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RecipientUserId",
                table: "chatmessages",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderUserId",
                table: "chatmessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtps_UserId",
                table: "emailotps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_ExternalMessageId",
                table: "mailmessages",
                column: "ExternalMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_ParentMessageId",
                table: "mailmessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_RecipientEmail",
                table: "mailmessages",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_RecipientUserId",
                table: "mailmessages",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_SenderEmail",
                table: "mailmessages",
                column: "SenderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_SenderUserId",
                table: "mailmessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "ContentKey",
                table: "sitecontents",
                column: "ContentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserName",
                table: "users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "FK_UserSecurityQuestions_SecurityQuestionMasters",
                table: "usersecurityquestions",
                column: "SecurityQuestionMasterId");

            migrationBuilder.CreateIndex(
                name: "FK_UserSecurityQuestions_Users",
                table: "usersecurityquestions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditlogs");

            migrationBuilder.DropTable(
                name: "ChatEmojis");

            migrationBuilder.DropTable(
                name: "chatmessages");

            migrationBuilder.DropTable(
                name: "emailotps");

            migrationBuilder.DropTable(
                name: "mailmessages");

            migrationBuilder.DropTable(
                name: "sitecontents");

            migrationBuilder.DropTable(
                name: "sitecontentsections");

            migrationBuilder.DropTable(
                name: "usersecurityquestions");

            migrationBuilder.DropTable(
                name: "securityquestionmasters");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
