using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchdayHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "competitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalId = table.Column<int>(type: "integer", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Season = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_competitions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "processed_messages",
            columns: table => new
            {
                MessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MessageType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processed_messages", x => x.MessageId);
            });

        migrationBuilder.CreateTable(
            name: "teams",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalId = table.Column<int>(type: "integer", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Stadium = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_teams", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "matches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalId = table.Column<int>(type: "integer", nullable: true),
                CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                AwayTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                KickoffUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                HomeScore = table.Column<int>(type: "integer", nullable: false),
                AwayScore = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_matches", x => x.Id);
                table.ForeignKey(
                    name: "FK_matches_competitions_CompetitionId",
                    column: x => x.CompetitionId,
                    principalTable: "competitions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_matches_teams_AwayTeamId",
                    column: x => x.AwayTeamId,
                    principalTable: "teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_matches_teams_HomeTeamId",
                    column: x => x.HomeTeamId,
                    principalTable: "teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "match_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UpstreamEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Minute = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_match_events", x => x.Id);
                table.ForeignKey(
                    name: "FK_match_events_matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_match_events_teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "player_match_loads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                Sprints = table.Column<int>(type: "integer", nullable: false),
                HighIntensityMinutes = table.Column<double>(type: "double precision", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_player_match_loads", x => x.Id);
                table.ForeignKey(
                    name: "FK_player_match_loads_matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_player_match_loads_teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_competitions_ExternalId",
            table: "competitions",
            column: "ExternalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_match_events_MatchId",
            table: "match_events",
            column: "MatchId");

        migrationBuilder.CreateIndex(
            name: "IX_match_events_TeamId",
            table: "match_events",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_match_events_UpstreamEventId",
            table: "match_events",
            column: "UpstreamEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_matches_AwayTeamId",
            table: "matches",
            column: "AwayTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_matches_CompetitionId",
            table: "matches",
            column: "CompetitionId");

        migrationBuilder.CreateIndex(
            name: "IX_matches_ExternalId",
            table: "matches",
            column: "ExternalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_matches_HomeTeamId",
            table: "matches",
            column: "HomeTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_matches_KickoffUtc",
            table: "matches",
            column: "KickoffUtc");

        migrationBuilder.CreateIndex(
            name: "IX_matches_Status",
            table: "matches",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_player_match_loads_MatchId",
            table: "player_match_loads",
            column: "MatchId");

        migrationBuilder.CreateIndex(
            name: "IX_player_match_loads_MatchId_PlayerName",
            table: "player_match_loads",
            columns: new[] { "MatchId", "PlayerName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_player_match_loads_TeamId",
            table: "player_match_loads",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_teams_ExternalId",
            table: "teams",
            column: "ExternalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_teams_Name",
            table: "teams",
            column: "Name");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "match_events");

        migrationBuilder.DropTable(
            name: "player_match_loads");

        migrationBuilder.DropTable(
            name: "processed_messages");

        migrationBuilder.DropTable(
            name: "matches");

        migrationBuilder.DropTable(
            name: "competitions");

        migrationBuilder.DropTable(
            name: "teams");
    }
}
