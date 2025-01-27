using Dapper;
using Microsoft.Extensions.Options;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class TournamentRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), ITournamentRepository
    {
        public async Task<int> CreateTournamentAsync(Tournament t)
        {
            const string query = @"
                INSERT INTO Tournaments 
                    (name, start_datetime, end_datetime, country_id, organizer_id)
                VALUES 
                    (@Name, @StartDatetime, @EndDateTime , @CountryId, @OrganizerId);
                SELECT LAST_INSERT_ID();";

            var parameters = new { t.Name, t.StartDateTime, t.EndDateTime, t.CountryId, t.OrganizerId };

            using var connection = CreateConnection();
            return await connection.QuerySingleAsync<int>(query, parameters);
        }

        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            const string query = @"
                    SELECT t.*, s.* FROM Tournaments t
                    LEFT JOIN tournament_series AS ts ON t.tournament_id = ts.tournament_id
                    LEFT JOIN series AS s ON ts.series_id = s.series_id
                    WHERE t.tournament_id = @TournamentId;";

            var parameters = new { TournamentId = id };

            using var connection = CreateConnection();

            Tournament? tournament = null;

            await connection.QueryAsync<Tournament, Serie, Tournament>(
                query,
                (t, s) =>
                {
                    if (tournament is null)
                    {
                        tournament = t;
                        tournament.Series = new List<Serie>();
                    }

                    if (s is not null)
                    {
                        tournament.Series.Add(s);
                    }
                    return t;
                },
                parameters,
                splitOn: "series_id");

            return tournament;
        }

        public async Task<bool> UpdateTournamentAsync(Tournament t)
        {
            const string query = @"
                UPDATE Tournaments 
                SET 
                    name = @Name,
                    start_datetime = @StartDatetime,
                    end_datetime = @EndDateTime,
                    country_id = @CountryId,
                    winner = @Winner,
                    organizer_id = @OrganizerId 
                WHERE tournament_id = @TournamentId;";

            var parameters = new
            {
                t.Name,
                t.StartDateTime,
                t.EndDateTime,
                t.CountryId,
                t.Winner,
                t.OrganizerId,
                t.TournamentId
            };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesId)
        {
            const string query = @"
                INSERT INTO tournament_series 
                    (tournament_id, series_id)
                SELECT
                    @TournamentId, series_id
                FROM series
                WHERE series_id IN @SeriesId;";

            var parameters = new { tournamentId, seriesId };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters);
        }
    }
}
