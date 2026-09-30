using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pino.Migrations;
/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "EF-generated schema declarations execute once per database migration; keeping column arrays beside each operation makes schema review reliable.")]
internal sealed partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                SecurityStamp = table.Column<string>(type: "text", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                PhoneNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Clubs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Sport = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clubs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PhotoDeletions",
            columns: table => new
            {
                PhotoKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                NotBefore = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhotoDeletions", x => x.PhotoKey);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleId = table.Column<string>(type: "text", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<string>(type: "text", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserPasskeys",
            columns: table => new
            {
                CredentialId = table.Column<byte[]>(type: "bytea", maxLength: 1024, nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                Data = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserPasskeys", x => x.CredentialId);
                table.ForeignKey(
                    name: "FK_AspNetUserPasskeys_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "text", nullable: false),
                RoleId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "text", nullable: false),
                LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClubProfiles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                PhotoKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClubProfiles", x => x.UserId);
                table.ForeignKey(
                    name: "FK_ClubProfiles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClubInvitations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                CreatedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UsedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClubInvitations", x => x.Id);
                table.UniqueConstraint("AK_ClubInvitations_ClubId_Id", x => new { x.ClubId, x.Id });
                table.CheckConstraint("CK_ClubInvitation_Role", "\"Role\" IN (0, 1)");
                table.ForeignKey(
                    name: "FK_ClubInvitations_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ClubJoinRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClubJoinRequests", x => x.Id);
                table.CheckConstraint("CK_ClubJoinRequest_Status", "\"Status\" IN (0, 1, 2, 3)");
                table.ForeignKey(
                    name: "FK_ClubJoinRequests_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ClubJoinRequests_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ClubMemberships",
            columns: table => new
            {
                UserId = table.Column<string>(type: "text", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClubMemberships", x => x.UserId);
                table.CheckConstraint("CK_ClubMembership_Role", "\"Role\" IN (0, 1)");
                table.ForeignKey(
                    name: "FK_ClubMemberships_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ClubMemberships_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PlayerImportReceipts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Created = table.Column<int>(type: "integer", nullable: false),
                Skipped = table.Column<int>(type: "integer", nullable: false),
                Reactivated = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlayerImportReceipts", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlayerImportReceipts_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerReference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                FirstName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                MiddleName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                GraduationYear = table.Column<int>(type: "integer", nullable: false),
                Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SecondaryPosition = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PhotoKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Archived = table.Column<bool>(type: "boolean", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.Id);
                table.UniqueConstraint("AK_Players_ClubId_Id", x => new { x.ClubId, x.Id });
                table.CheckConstraint("CK_Player_Year", "\"GraduationYear\" BETWEEN 2000 AND 2100");
                table.ForeignKey(
                    name: "FK_Players_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Seasons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                Archived = table.Column<bool>(type: "boolean", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Seasons", x => x.Id);
                table.UniqueConstraint("AK_Seasons_ClubId_Id", x => new { x.ClubId, x.Id });
                table.CheckConstraint("CK_Season_Dates", "\"StartsOn\" <= \"EndsOn\"");
                table.ForeignKey(
                    name: "FK_Seasons_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SportingBatchReceipts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Applied = table.Column<int>(type: "integer", nullable: false),
                Skipped = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SportingBatchReceipts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SportingBatchReceipts_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SportTeams",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                GraduationYear = table.Column<int>(type: "integer", nullable: false),
                Archived = table.Column<bool>(type: "boolean", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                RosterTarget = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SportTeams", x => x.Id);
                table.UniqueConstraint("AK_SportTeams_ClubId_Id", x => new { x.ClubId, x.Id });
                table.CheckConstraint("CK_Team_RosterTarget", "\"RosterTarget\" IS NULL OR \"RosterTarget\" BETWEEN 0 AND 2000");
                table.CheckConstraint("CK_Team_Year", "\"GraduationYear\" BETWEEN 2000 AND 2100");
                table.ForeignKey(
                    name: "FK_SportTeams_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PlayerErasures",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PhotoKey = table.Column<string>(type: "character varying(100)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlayerErasures", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlayerErasures_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PlayerErasures_PhotoDeletions_PhotoKey",
                    column: x => x.PhotoKey,
                    principalTable: "PhotoDeletions",
                    principalColumn: "PhotoKey",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "StaffEmails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientId = table.Column<string>(type: "text", nullable: true),
                RecipientEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                NormalizedRecipientEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ProtectedBody = table.Column<string>(type: "text", nullable: false),
                InvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                InvitationRevision = table.Column<long>(type: "bigint", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffEmails", x => x.Id);
                table.CheckConstraint("CK_StaffEmail_Status", "\"Status\" IN (0, 1, 2, 3, 4)");
                table.ForeignKey(
                    name: "FK_StaffEmails_AspNetUsers_RecipientId",
                    column: x => x.RecipientId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StaffEmails_ClubInvitations_ClubId_InvitationId",
                    columns: x => new { x.ClubId, x.InvitationId },
                    principalTable: "ClubInvitations",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_StaffEmails_Clubs_ClubId",
                    column: x => x.ClubId,
                    principalTable: "Clubs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TryoutEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Location = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                Closed = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TryoutEvents", x => x.Id);
                table.UniqueConstraint("AK_TryoutEvents_ClubId_Id", x => new { x.ClubId, x.Id });
                table.ForeignKey(
                    name: "FK_TryoutEvents_Seasons_ClubId_SeasonId",
                    columns: x => new { x.ClubId, x.SeasonId },
                    principalTable: "Seasons",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SeasonTeamAvailabilities",
            columns: table => new
            {
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                Excluded = table.Column<bool>(type: "boolean", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SeasonTeamAvailabilities", x => new { x.ClubId, x.SeasonId, x.TeamId });
                table.ForeignKey(
                    name: "FK_SeasonTeamAvailabilities_Seasons_ClubId_SeasonId",
                    columns: x => new { x.ClubId, x.SeasonId },
                    principalTable: "Seasons",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SeasonTeamAvailabilities_SportTeams_ClubId_TeamId",
                    columns: x => new { x.ClubId, x.TeamId },
                    principalTable: "SportTeams",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TeamPositionTargets",
            columns: table => new
            {
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                PositionKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Players = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamPositionTargets", x => new { x.ClubId, x.TeamId, x.PositionKey });
                table.CheckConstraint("CK_PositionTarget_Count", "\"Players\" BETWEEN 0 AND 2000");
                table.ForeignKey(
                    name: "FK_TeamPositionTargets_SportTeams_ClubId_TeamId",
                    columns: x => new { x.ClubId, x.TeamId },
                    principalTable: "SportTeams",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Participations",
            columns: table => new
            {
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                Bib = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Decision = table.Column<int>(type: "integer", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                Removed = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Participations", x => new { x.ClubId, x.TryoutId, x.PlayerId });
                table.CheckConstraint("CK_Participation_Decision", "\"Decision\" BETWEEN 0 AND 4 AND ((\"Decision\" = 1) = (\"TeamId\" IS NOT NULL))");
                table.ForeignKey(
                    name: "FK_Participations_Players_ClubId_PlayerId",
                    columns: x => new { x.ClubId, x.PlayerId },
                    principalTable: "Players",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Participations_SportTeams_ClubId_TeamId",
                    columns: x => new { x.ClubId, x.TeamId },
                    principalTable: "SportTeams",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Participations_TryoutEvents_ClubId_TryoutId",
                    columns: x => new { x.ClubId, x.TryoutId },
                    principalTable: "TryoutEvents",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SeasonPlacements",
            columns: table => new
            {
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: true),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SeasonPlacements", x => new { x.ClubId, x.SeasonId, x.PlayerId });
                table.ForeignKey(
                    name: "FK_SeasonPlacements_Players_ClubId_PlayerId",
                    columns: x => new { x.ClubId, x.PlayerId },
                    principalTable: "Players",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SeasonPlacements_Seasons_ClubId_SeasonId",
                    columns: x => new { x.ClubId, x.SeasonId },
                    principalTable: "Seasons",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SeasonPlacements_SportTeams_ClubId_TeamId",
                    columns: x => new { x.ClubId, x.TeamId },
                    principalTable: "SportTeams",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SeasonPlacements_TryoutEvents_ClubId_TryoutId",
                    columns: x => new { x.ClubId, x.TryoutId },
                    principalTable: "TryoutEvents",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TryoutCloseouts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                SeasonName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                TryoutDate = table.Column<DateOnly>(type: "date", nullable: false),
                ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ClosedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                ClosedBy = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                ReopenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ReopenedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                ReopenedBy = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true),
                ReopenReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ErasedPlayers = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TryoutCloseouts", x => x.Id);
                table.ForeignKey(
                    name: "FK_TryoutCloseouts_TryoutEvents_ClubId_TryoutId",
                    columns: x => new { x.ClubId, x.TryoutId },
                    principalTable: "TryoutEvents",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TryoutTeamAvailabilities",
            columns: table => new
            {
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                Excluded = table.Column<bool>(type: "boolean", nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TryoutTeamAvailabilities", x => new { x.ClubId, x.TryoutId, x.TeamId });
                table.ForeignKey(
                    name: "FK_TryoutTeamAvailabilities_SportTeams_ClubId_TeamId",
                    columns: x => new { x.ClubId, x.TeamId },
                    principalTable: "SportTeams",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TryoutTeamAvailabilities_TryoutEvents_ClubId_TryoutId",
                    columns: x => new { x.ClubId, x.TryoutId },
                    principalTable: "TryoutEvents",
                    principalColumns: new[] { "ClubId", "Id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DecisionEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                SeasonName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Kind = table.Column<int>(type: "integer", nullable: false),
                TeamName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                PreviousTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                Author = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DecisionEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_DecisionEvents_Participations_ClubId_TryoutId_PlayerId",
                    columns: x => new { x.ClubId, x.TryoutId, x.PlayerId },
                    principalTable: "Participations",
                    principalColumns: new[] { "ClubId", "TryoutId", "PlayerId" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EnrollmentChanges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                Removed = table.Column<bool>(type: "boolean", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                Author = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Bib = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EnrollmentChanges", x => x.Id);
                table.ForeignKey(
                    name: "FK_EnrollmentChanges_Participations_ClubId_TryoutId_PlayerId",
                    columns: x => new { x.ClubId, x.TryoutId, x.PlayerId },
                    principalTable: "Participations",
                    principalColumns: new[] { "ClubId", "TryoutId", "PlayerId" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PlayerNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                TryoutId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                Author = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CorrectsId = table.Column<Guid>(type: "uuid", nullable: true),
                RedactionOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                RedactedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RedactedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                RedactedBy = table.Column<string>(type: "character varying(161)", maxLength: 161, nullable: true),
                RedactionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlayerNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlayerNotes_Participations_ClubId_TryoutId_PlayerId",
                    columns: x => new { x.ClubId, x.TryoutId, x.PlayerId },
                    principalTable: "Participations",
                    principalColumns: new[] { "ClubId", "TryoutId", "PlayerId" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TryoutCloseoutPlayers",
            columns: table => new
            {
                CloseoutId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                MiddleName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                GraduationYear = table.Column<int>(type: "integer", nullable: false),
                Bib = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Decision = table.Column<int>(type: "integer", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                TeamName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TryoutCloseoutPlayers", x => new { x.CloseoutId, x.PlayerId });
                table.ForeignKey(
                    name: "FK_TryoutCloseoutPlayers_TryoutCloseouts_CloseoutId",
                    column: x => x.CloseoutId,
                    principalTable: "TryoutCloseouts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserPasskeys_UserId",
            table: "AspNetUserPasskeys",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClubInvitations_ClubId_NormalizedEmail",
            table: "ClubInvitations",
            columns: new[] { "ClubId", "NormalizedEmail" });

        migrationBuilder.CreateIndex(
            name: "IX_ClubJoinRequests_ClubId_Status_CreatedAt",
            table: "ClubJoinRequests",
            columns: new[] { "ClubId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ClubJoinRequests_UserId",
            table: "ClubJoinRequests",
            column: "UserId",
            unique: true,
            filter: "\"Status\" = 0");

        migrationBuilder.CreateIndex(
            name: "IX_ClubMemberships_ClubId_Role",
            table: "ClubMemberships",
            columns: new[] { "ClubId", "Role" });

        migrationBuilder.CreateIndex(
            name: "IX_Clubs_CreatedBy_OperationId",
            table: "Clubs",
            columns: new[] { "CreatedBy", "OperationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DecisionEvents_ClubId_PlayerId_CreatedAt",
            table: "DecisionEvents",
            columns: new[] { "ClubId", "PlayerId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DecisionEvents_ClubId_TryoutId_PlayerId",
            table: "DecisionEvents",
            columns: new[] { "ClubId", "TryoutId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_EnrollmentChanges_ClubId_TryoutId_PlayerId",
            table: "EnrollmentChanges",
            columns: new[] { "ClubId", "TryoutId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_Participations_ClubId_PlayerId",
            table: "Participations",
            columns: new[] { "ClubId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_Participations_ClubId_TeamId",
            table: "Participations",
            columns: new[] { "ClubId", "TeamId" });

        migrationBuilder.CreateIndex(
            name: "IX_Participations_ClubId_TryoutId_Bib",
            table: "Participations",
            columns: new[] { "ClubId", "TryoutId", "Bib" },
            unique: true,
            filter: "\"Bib\" <> '' AND NOT \"Removed\"");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerErasures_ClubId",
            table: "PlayerErasures",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerErasures_PhotoKey",
            table: "PlayerErasures",
            column: "PhotoKey");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerImportReceipts_ClubId",
            table: "PlayerImportReceipts",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerNotes_ClubId_RedactionOperationId",
            table: "PlayerNotes",
            columns: new[] { "ClubId", "RedactionOperationId" });

        migrationBuilder.CreateIndex(
            name: "IX_PlayerNotes_ClubId_TryoutId_PlayerId",
            table: "PlayerNotes",
            columns: new[] { "ClubId", "TryoutId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_PlayerNotes_CorrectsId",
            table: "PlayerNotes",
            column: "CorrectsId",
            unique: true,
            filter: "\"CorrectsId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Players_ClubId_PlayerReference",
            table: "Players",
            columns: new[] { "ClubId", "PlayerReference" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPlacements_ClubId_PlayerId",
            table: "SeasonPlacements",
            columns: new[] { "ClubId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPlacements_ClubId_TeamId",
            table: "SeasonPlacements",
            columns: new[] { "ClubId", "TeamId" });

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPlacements_ClubId_TryoutId",
            table: "SeasonPlacements",
            columns: new[] { "ClubId", "TryoutId" });

        migrationBuilder.CreateIndex(
            name: "IX_SeasonTeamAvailabilities_ClubId_TeamId",
            table: "SeasonTeamAvailabilities",
            columns: new[] { "ClubId", "TeamId" });

        migrationBuilder.CreateIndex(
            name: "IX_SportingBatchReceipts_ClubId",
            table: "SportingBatchReceipts",
            column: "ClubId");

        migrationBuilder.CreateIndex(
            name: "IX_StaffEmails_ClubId_CreatedAt",
            table: "StaffEmails",
            columns: new[] { "ClubId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffEmails_ClubId_InvitationId",
            table: "StaffEmails",
            columns: new[] { "ClubId", "InvitationId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffEmails_NormalizedRecipientEmail",
            table: "StaffEmails",
            column: "NormalizedRecipientEmail");

        migrationBuilder.CreateIndex(
            name: "IX_StaffEmails_RecipientId",
            table: "StaffEmails",
            column: "RecipientId");

        migrationBuilder.CreateIndex(
            name: "IX_StaffEmails_Status_NextAttemptAt",
            table: "StaffEmails",
            columns: new[] { "Status", "NextAttemptAt" });

        migrationBuilder.CreateIndex(
            name: "IX_TryoutCloseouts_ClubId_TryoutId_ClosedAt",
            table: "TryoutCloseouts",
            columns: new[] { "ClubId", "TryoutId", "ClosedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_TryoutEvents_ClubId_SeasonId",
            table: "TryoutEvents",
            columns: new[] { "ClubId", "SeasonId" });

        migrationBuilder.CreateIndex(
            name: "IX_TryoutTeamAvailabilities_ClubId_TeamId",
            table: "TryoutTeamAvailabilities",
            columns: new[] { "ClubId", "TeamId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserPasskeys");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "ClubJoinRequests");

        migrationBuilder.DropTable(
            name: "ClubMemberships");

        migrationBuilder.DropTable(
            name: "ClubProfiles");

        migrationBuilder.DropTable(
            name: "DecisionEvents");

        migrationBuilder.DropTable(
            name: "EnrollmentChanges");

        migrationBuilder.DropTable(
            name: "PlayerErasures");

        migrationBuilder.DropTable(
            name: "PlayerImportReceipts");

        migrationBuilder.DropTable(
            name: "PlayerNotes");

        migrationBuilder.DropTable(
            name: "SeasonPlacements");

        migrationBuilder.DropTable(
            name: "SeasonTeamAvailabilities");

        migrationBuilder.DropTable(
            name: "SportingBatchReceipts");

        migrationBuilder.DropTable(
            name: "StaffEmails");

        migrationBuilder.DropTable(
            name: "TeamPositionTargets");

        migrationBuilder.DropTable(
            name: "TryoutCloseoutPlayers");

        migrationBuilder.DropTable(
            name: "TryoutTeamAvailabilities");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "PhotoDeletions");

        migrationBuilder.DropTable(
            name: "Participations");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "ClubInvitations");

        migrationBuilder.DropTable(
            name: "TryoutCloseouts");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropTable(
            name: "SportTeams");

        migrationBuilder.DropTable(
            name: "TryoutEvents");

        migrationBuilder.DropTable(
            name: "Seasons");

        migrationBuilder.DropTable(
            name: "Clubs");
    }
}
