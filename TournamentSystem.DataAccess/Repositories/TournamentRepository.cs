using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.DataAccess.Repositories
{
    public class TournamentRepository(IOptions<ConnectionStrings> options) : BaseRepository(options), ITournamentRepository
    {
        public async Task<int> CreateTournamentAsync(Tournament t)
        {
            const string query = @"
                INSERT INTO Tournaments 
                    (name, start_datetime, end_datetime, country_code, organizer_id)
                VALUES 
                    (@Name, @StartDatetime, @EndDateTime , @CountryCode, @OrganizerId);
                SELECT LAST_INSERT_ID();";

            var parameters = new { t.Name, t.StartDateTime, t.EndDateTime, t.CountryCode, t.OrganizerId };

            using var connection = CreateConnection();
            connection.Open();
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    t.TournamentId = await connection.QuerySingleAsync<int>(query, parameters, transaction);

                    await AddSeriesToTournamentAsync(connection, transaction, t.TournamentId, t.Series.Select(s => s.SeriesId).ToArray());

                    transaction.Commit();
                    return t.TournamentId;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private async Task<int> AddSeriesToTournamentAsync(IDbConnection connection, IDbTransaction? transaction, int tournamentId, int[] seriesIds)
        {
            const string query = @"
                INSERT INTO tournament_series 
                    (tournament_id, series_id)
                SELECT
                    @TournamentId, series_id
                FROM series
                WHERE series_id IN @SeriesIds;";

            var parameters = new { tournamentId, seriesIds };

            return await connection.ExecuteAsync(query, parameters, transaction);
        }

        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            const string query = @"
                    SELECT t.*, s.*, p.*, ju.*, o.* FROM Tournaments t
                    LEFT JOIN tournament_series AS ts ON t.tournament_id = ts.tournament_id
                    LEFT JOIN series AS s ON ts.series_id = s.series_id
                    LEFT JOIN tournament_players AS tp ON t.tournament_id = tp.tournament_id
                    LEFT JOIN users AS p ON tp.user_id = p.user_id
                    LEFT JOIN tournament_judges AS tj ON t.tournament_id = tj.tournament_id
                    LEFT JOIN users AS ju ON tj.user_id = ju.user_id
                    LEFT JOIN users AS o ON t.organizer_id = o.user_id
                    WHERE t.tournament_id = @TournamentId;";

            var parameters = new { TournamentId = id };

            using var connection = CreateConnection();

            var tournamentDictionary = new Dictionary<int, Tournament>();

            var tournament = await connection.QueryAsync<Tournament, Serie, User, User, User, Tournament>(
                query,
                (t, serie, player, judge, organizer) =>
                {
                    if (!tournamentDictionary.TryGetValue(t.TournamentId, out var tournamentEntry))
                    {
                        tournamentEntry = t;
                        tournamentEntry.Series = [];
                        tournamentEntry.Players = [];
                        tournamentEntry.Judges = [];
                        tournamentEntry.Organizer = organizer;
                        tournamentDictionary.Add(tournamentEntry.TournamentId, tournamentEntry);
                    }

                    if (serie is not null && !tournamentEntry.Series.Any(s => s.SeriesId == serie.SeriesId))
                        tournamentEntry.Series.Add(serie);

                    if (player is not null && !tournamentEntry.Players.Any(p => p.UserId == player.UserId))
                        tournamentEntry.Players.Add(player);

                    if (judge is not null && !tournamentEntry.Judges.Any(j => j.UserId == judge.UserId))
                        tournamentEntry.Judges.Add(judge);

                    return tournamentEntry;
                },
                parameters,
                splitOn: "series_id, user_id,user_id,user_id");

            return tournamentDictionary.Values.FirstOrDefault();
        }

        public async Task<bool> UpdateTournamentAsync(Tournament t)
        {
            const string query = @"
                UPDATE Tournaments 
                SET 
                    name = @Name,
                    start_datetime = @StartDatetime,
                    end_datetime = @EndDateTime,
                    country_code = @CountryCode,
                    winner = @Winner,
                    organizer_id = @OrganizerId 
                WHERE tournament_id = @TournamentId;";

            var parameters = new
            {
                t.Name,
                t.StartDateTime,
                t.EndDateTime,
                t.CountryCode,
                t.Winner,
                t.OrganizerId,
                t.TournamentId
            };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesIds)
        {
            using var connection = CreateConnection();
            return await AddSeriesToTournamentAsync(connection, null, tournamentId, seriesIds);
        }

        public async Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            const string registerPlayerQuery = @"
                INSERT INTO tournament_players 
                    (tournament_id, user_id)
                VALUES 
                    (@TournamentId, @PlayerId);";

            const string createDeckQuery = @"
                INSERT INTO decks 
                    (user_id, tournament_id)
                VALUES 
                    (@PlayerId, @TournamentId);
                SELECT LAST_INSERT_ID();";

            const string addDeckCardsQuery = @"
                INSERT INTO deck_cards
                    (deck_id, card_id)
                SELECT
                    @DeckId, card_id
                FROM cards
                WHERE card_id IN @CardsIds;";


            var parameters = new { tournamentId, playerId };

            using var connection = CreateConnection();
            connection.Open();
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    await connection.ExecuteAsync(registerPlayerQuery, new { tournamentId, playerId }, transaction);

                    var deckId = await connection.QuerySingleAsync<int>(createDeckQuery, new { playerId, tournamentId }, transaction);

                    await connection.ExecuteAsync(addDeckCardsQuery, new { deckId, cardsIds }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId)
        {
            const string query = @"
                INSERT INTO tournament_judges 
                    (tournament_id, user_id)
                VALUES 
                    (@TournamentId, @JudgeId);";

            var parameters = new { tournamentId, judgeId };

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<int> GetPlayerCountAsync(int tournamentId)
        {
            const string query = @"
            SELECT COUNT(*)
            FROM tournament_players
            WHERE tournament_id = @TournamentId;";

            var parameters = new { TournamentId = tournamentId };

            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(query, parameters);
        }

        public async Task<bool> FinalizeRegistrationAndStartTournamentAsync(int tournamentId, List<Game> games)
        {
            const string updateTournamentQuery = @"
                UPDATE Tournaments 
                SET 
                    phase = @Phase
                WHERE tournament_id = @TournamentId;";

            var tournamentParameters = new { Phase = nameof(TournamentPhase.Tournament).ToLower(), tournamentId };

            const string addGamesQuery = @"
                INSERT INTO games
                    (tournament_id, round_number, start_time, player1_id, player2_id)
                VALUES
                    (@TournamentId, @RoundNumber, @StartTime, @Player1Id, @Player2Id)";

            using var connection = CreateConnection();
            connection.Open();
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    await connection.ExecuteAsync(updateTournamentQuery, tournamentParameters, transaction);

                    await connection.ExecuteAsync(addGamesQuery, games, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            throw new NotImplementedException();
        }
    }
}
