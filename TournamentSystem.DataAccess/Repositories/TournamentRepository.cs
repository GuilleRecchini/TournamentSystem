using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
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

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            {
                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    t.TournamentId = await connection.QuerySingleAsync<int>(query, parameters, transaction);

                    await AddSeriesToTournamentAsync(connection, transaction, t.TournamentId, t.Series.Select(s => s.SeriesId).ToArray());

                    await AssignJudgesToTournamentAsync(connection, transaction, t.TournamentId, t.Judges.Select(j => j.UserId).ToArray());

                    await transaction.CommitAsync();
                    return t.TournamentId;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static async Task<int> AddSeriesToTournamentAsync(IDbConnection connection, IDbTransaction? transaction, int tournamentId, int[] seriesIds)
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

        private static async Task<int> AssignJudgesToTournamentAsync(IDbConnection connection, IDbTransaction transaction, int tournamentId, int[] judgesIds)
        {
            const string query = @"
                INSERT INTO tournament_judges 
                    (tournament_id, user_id)
                SELECT
                    @TournamentId, user_id
                FROM Users
                WHERE user_id IN @JudgesIds";

            var parameters = new { tournamentId, judgesIds };
            return await connection.ExecuteAsync(query, parameters, transaction);
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsAsync(int? id = null, int? organizerId = null, int[]? judgeIds = null, TournamentPhase? phase = null, bool? isCanceled = null)
        {
            var queryBuilder = new StringBuilder(@"
                SELECT t.*, s.*, p.*, w.*, ju.*, o.*, g.* 
                FROM Tournaments t
                LEFT JOIN tournament_series AS ts ON t.tournament_id = ts.tournament_id
                LEFT JOIN series AS s ON ts.series_id = s.series_id
                LEFT JOIN tournament_players AS tp ON t.tournament_id = tp.tournament_id
                LEFT JOIN users AS p ON tp.user_id = p.user_id
                LEFT JOIN users AS w ON t.winner_id = w.user_id
                LEFT JOIN tournament_judges AS tj ON t.tournament_id = tj.tournament_id
                LEFT JOIN users AS ju ON tj.user_id = ju.user_id
                LEFT JOIN users AS o ON t.organizer_id = o.user_id
                LEFT JOIN games AS g ON t.tournament_id = g.tournament_id");

            var parameters = new DynamicParameters();
            var conditions = new List<string>();

            if (isCanceled is not null)
            {
                conditions.Add("t.is_canceled = @IsCanceled");
                parameters.Add("IsCanceled", isCanceled.Value);
            }

            if (id.HasValue)
            {
                conditions.Add("t.tournament_id = @TournamentId");
                parameters.Add("TournamentId", id.Value);
            }

            if (organizerId.HasValue)
            {
                conditions.Add("o.user_id = @OrganizerId");
                parameters.Add("OrganizerId", organizerId.Value);
            }

            if (judgeIds is { Length: > 0 })
            {
                conditions.Add("ju.user_id IN @JudgeIds");
                parameters.Add("JudgeIds", judgeIds);
            }

            if (phase.HasValue)
            {
                conditions.Add("t.phase = @Phase");
                parameters.Add("Phase", phase.Value.ToString());
            }

            if (conditions.Count > 0)
            {
                queryBuilder.Append(" WHERE ").AppendJoin(" AND ", conditions);
            }

            await using var connection = CreateConnection();
            var tournamentDictionary = new Dictionary<int, Tournament>();

            var tournament = await connection.QueryAsync<Tournament, Serie, User, User, User, User, Game, Tournament>(
                queryBuilder.ToString(),
                (t, serie, player, winner, judge, organizer, game) =>
                {
                    if (!tournamentDictionary.TryGetValue(t.TournamentId, out var tournamentEntry))
                    {
                        tournamentEntry = t;
                        tournamentEntry.Series = [];
                        tournamentEntry.Players = [];
                        tournamentEntry.Winner = winner;
                        tournamentEntry.Judges = [];
                        tournamentEntry.Organizer = organizer;
                        tournamentEntry.Games = [];
                        tournamentDictionary.Add(tournamentEntry.TournamentId, tournamentEntry);
                    }

                    if (serie is not null && !tournamentEntry.Series.Any(s => s.SeriesId == serie.SeriesId))
                        tournamentEntry.Series.Add(serie);

                    if (player is not null && !tournamentEntry.Players.Any(p => p.UserId == player.UserId))
                        tournamentEntry.Players.Add(player);

                    if (judge is not null && !tournamentEntry.Judges.Any(j => j.UserId == judge.UserId))
                        tournamentEntry.Judges.Add(judge);

                    if (game is not null && !tournamentEntry.Games.Any(g => g.GameId == game.GameId))
                        tournamentEntry.Games.Add(game);

                    return tournamentEntry;
                },
                parameters,
                splitOn: "series_id, user_id,user_id,user_id,user_id,game_id");


            foreach (var tournamentEntry in tournamentDictionary.Values)
            {
                tournamentEntry.Games.Sort((g1, g2) => g1.GameId.CompareTo(g2.GameId));
            }

            return tournamentDictionary.Values.ToList();
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
                t.WinnerId,
                t.OrganizerId,
                t.TournamentId
            };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesIds)
        {
            await using var connection = CreateConnection();
            return await AddSeriesToTournamentAsync(connection, null, tournamentId, seriesIds);
        }

        public async Task<User> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds)
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

            const string getUserQuery = @"
                SELECT *
                FROM users
                WHERE user_id = @PlayerId;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            {
                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    await connection.ExecuteAsync(registerPlayerQuery, new { tournamentId, playerId }, transaction);

                    var deckId = await connection.QuerySingleAsync<int>(createDeckQuery, new { playerId, tournamentId }, transaction);

                    await connection.ExecuteAsync(addDeckCardsQuery, new { deckId, cardsIds }, transaction);

                    var player = await connection.QueryFirstAsync<User>(getUserQuery, new { playerId }, transaction);

                    await transaction.CommitAsync();

                    return player;
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

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<int> GetPlayerCountAsync(int tournamentId)
        {
            const string query = @"
            SELECT COUNT(*)
            FROM tournament_players
            WHERE tournament_id = @TournamentId;";

            var parameters = new { TournamentId = tournamentId };

            await using var connection = CreateConnection();
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
                    (tournament_id, start_time, player1_id, player2_id)
                VALUES
                    (@TournamentId, @StartTime, @Player1Id, @Player2Id)";

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            {
                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    await connection.ExecuteAsync(updateTournamentQuery, tournamentParameters, transaction);

                    await connection.ExecuteAsync(addGamesQuery, games, transaction);

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<bool> SetGameWinnerAsync(int gameId, int winnerId)
        {
            const string query = @"
                UPDATE Games
                SET 
                    winner_id = @WinnerId
                WHERE game_id = @GameId;";

            var parameters = new { GameId = gameId, WinnerId = winnerId };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;

        }

        public async Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int disqualifiedBy)
        {
            const string query = @"
                INSERT INTO disqualifications
                    (user_id, tournament_id, reason, disqualified_by)
                VALUES
                    (@PlayerId, @TournamentId, @Reason, @DisqualifiedBy);";

            var parameters = new { PlayerId = playerId, TournamentId = tournamentId, Reason = reason, DisqualifiedBy = disqualifiedBy };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;

            throw new NotImplementedException();
        }

        public async Task<bool> AdvanceWinnersToNextRoundAsync(List<Game> games)
        {
            const string query = @"
                UPDATE Games
                SET 
                    player1_id = @Player1Id,
                    player2_id = @Player2Id
                WHERE game_id = @GameId;";
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            {
                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    await connection.ExecuteAsync(query, games, transaction);
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<bool> FinalizeTournamentAsync(int tournamentId)
        {
            const string query = @"
                UPDATE Tournaments 
                SET 
                    phase = @Phase
                WHERE tournament_id = @TournamentId;";

            var parameters = new { Phase = nameof(TournamentPhase.Completion).ToLower(), tournamentId };

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, parameters) > 0;
        }

        public async Task<Deck?> GetPlayerDeckByTournamentIdAsync(int tournamentId, int playerId)
        {
            const string query = @"
                SELECT d.*, t.*, s.*, c.*, s2.*
                FROM decks d
                JOIN tournaments t ON d.tournament_id = t.tournament_id
                JOIN tournament_series ts ON t.tournament_id = ts.tournament_id 
                JOIN series s ON ts.series_id = s.series_id
                JOIN deck_cards dc ON d.deck_id = dc.deck_id
                JOIN cards c ON dc.card_id = c.card_id
                JOIN card_series cs ON c.card_id = cs.card_id
                JOIN series s2 ON cs.series_id = s2.series_id
                WHERE d.user_id = @PlayerId 
                AND d.tournament_id = @TournamentId
                AND t.is_canceled = 0;";

            await using var connection = CreateConnection();
            var deckDictionary = new Dictionary<int, Deck>();
            var cardDictionary = new Dictionary<int, Card>();

            var deck = (await connection.QueryAsync<Deck, Tournament, Serie, Card, Serie, Deck>(
                query,
                (d, t, s, c, s2) =>
                {
                    if (!deckDictionary.TryGetValue(d.DeckId, out var deckEntry))
                    {
                        deckEntry = d;
                        deckEntry.Tournament = t;
                        deckEntry.Tournament.Series = [];
                        deckEntry.Cards = [];
                        deckDictionary.Add(deckEntry.DeckId, deckEntry);
                    }
                    if (s is not null && !deckEntry.Tournament.Series.Any(serie => serie.SeriesId == s.SeriesId))
                        deckEntry.Tournament.Series.Add(s);

                    if (c is not null)
                    {
                        if (!cardDictionary.TryGetValue(c.CardId, out var cardEntry))
                        {
                            cardEntry = c;
                            cardEntry.Series = [];
                            cardDictionary.Add(cardEntry.CardId, cardEntry);
                        }

                        if (s2 is not null && !cardEntry.Series.Any(serie => serie.SeriesId == s2.SeriesId))
                            cardEntry.Series.Add(s2);
                    }

                    return deckEntry;
                },
                new { PlayerId = playerId, TournamentId = tournamentId },
                splitOn: "tournament_id, series_id, card_id,series_id")).FirstOrDefault();

            if (deck is not null)
                deck.Cards.AddRange(cardDictionary.Values);

            return deck;
        }

        public async Task<int> AddCardsToDeckAsync(int deckId, int[] cardsIds)
        {
            const string addDeckCardsQuery = @"
                INSERT INTO deck_cards
                    (deck_id, card_id)
                SELECT
                    @DeckId, card_id
                FROM cards
                WHERE card_id IN @CardsIds
                AND NOT EXISTS (
                    SELECT 1 FROM deck_cards WHERE deck_id = @DeckId AND card_id = cards.card_id
                )";

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(addDeckCardsQuery, new { deckId, cardsIds });
        }

        public async Task<int> RemoveCardsFromDeckAsync(int deckId, int[] cardsIds)
        {
            const string query = @"
                DELETE FROM deck_cards
                WHERE deck_id = @DeckId
                AND card_id IN @CardsIds;";

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, new { deckId, cardsIds });
        }

        public async Task<bool> CancelTournamentAsync(int tournamentId)
        {
            const string query = @"
                UPDATE Tournaments 
                SET 
                    is_canceled = 1
                WHERE tournament_id = @TournamentId;";

            await using var connection = CreateConnection();
            return await connection.ExecuteAsync(query, new { TournamentId = tournamentId }) > 0;
        }
    }
}
