using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Interfaces;
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
                    (name, start_datetime, end_datetime, country_code, organizer_id, max_players)
                VALUES 
                    (@Name, @StartDatetime, @EndDateTime , @CountryCode, @OrganizerId, @MaxPlayers);
                SELECT LAST_INSERT_ID();";

            var parameters = new { t.Name, t.StartDateTime, t.EndDateTime, t.CountryCode, t.OrganizerId, t.MaxPlayers };

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

        public async Task<Tournament?> GetTournamentByIdAsync(int tournamnetId, TournamentsFilter? filter)
        {
            return (await GetAllTournamentsAsync(filter, tournamnetId)).FirstOrDefault();
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsAsync(TournamentsFilter? filter)
        {
            return await GetAllTournamentsAsync(filter);
        }

        private async Task<IEnumerable<Tournament>> GetAllTournamentsAsync(TournamentsFilter? filter = null, int? tournamnetId = null)
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

            filter ??= new TournamentsFilter();

            if (tournamnetId is not null)
            {
                conditions.Add("t.tournament_id = @TournamentId");
                parameters.Add("TournamentId", tournamnetId);
            }

            if (filter.IsCanceled is not null)
            {
                conditions.Add("t.is_canceled = @IsCanceled");
                parameters.Add("IsCanceled", filter.IsCanceled);
            }

            if (filter.OrganizerId is not null)
            {
                conditions.Add("o.user_id = @OrganizerId");
                parameters.Add("OrganizerId", filter.OrganizerId);
            }

            if (filter.JudgeIds is not null)
            {
                conditions.Add("ju.user_id IN @JudgeIds");
                parameters.Add("JudgeIds", filter.JudgeIds);
            }

            if (filter.Phase is not null)
            {
                conditions.Add("t.phase = @Phase");
                parameters.Add("Phase", filter.Phase.ToString());
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
            const string updateTournamentQuery = @"
                UPDATE Tournaments 
                SET 
                    name = @Name,
                    country_code = @CountryCode,
                    organizer_id = @OrganizerId 
                WHERE tournament_id = @TournamentId;";

            const string updateJudgesQuery = @"        
                DELETE FROM tournament_judges 
                WHERE tournament_id = @TournamentId 
                AND user_id NOT IN @JudgesIds;
        
                INSERT INTO tournament_judges (tournament_id, user_id)
                SELECT @TournamentId, user_id 
                FROM Users
                WHERE user_id IN @JudgesIds
                AND user_id NOT IN (SELECT user_id FROM tournament_judges WHERE tournament_id = @TournamentId);";

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(updateTournamentQuery, new { t.Name, t.CountryCode, t.OrganizerId, t.TournamentId }, transaction);

                await connection.ExecuteAsync(updateJudgesQuery, new { t.TournamentId, JudgesIds = t.Judges.Select(j => j.UserId).ToArray() }, transaction);

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            const string disqualifyPlayerQuery = @"
                INSERT INTO disqualifications
                    (user_id, tournament_id, reason, disqualified_by)
                VALUES
                    (@PlayerId, @TournamentId, @Reason, @DisqualifiedBy);";

            const string getGameQuery = @"
                    SELECT *
                    FROM Games
                    WHERE (player1_id = @PlayerId OR player2_id = @PlayerId) 
                    AND winner_id IS NULL
                    AND tournament_id = @TournamentId;";

            const string setWinnerQuery = @"
                UPDATE Games
                SET 
                    winner_id = @WinnerId
                WHERE game_id = @GameId;";

            var parameters = new { PlayerId = playerId, TournamentId = tournamentId, Reason = reason, DisqualifiedBy = disqualifiedBy };

            await using var connection = CreateConnection();

            var result = await connection.ExecuteAsync(disqualifyPlayerQuery, parameters) > 0;

            var game = await connection.QueryFirstOrDefaultAsync<Game>(getGameQuery, new { PlayerId = playerId, TournamentId = tournamentId });

            if (game is not null)
            {
                var winnerId = game.Player1Id == playerId ? game.Player2Id : game.Player1Id;
                await connection.ExecuteAsync(setWinnerQuery, new { WinnerId = winnerId, game.GameId });
            }

            return result;
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
            return (await GetDecksByTournamentIdAsync(tournamentId, playerId)).FirstOrDefault();
        }

        public async Task<IEnumerable<Deck>> GetTournamentDecksAsync(int tournamentId)
        {
            return await GetDecksByTournamentIdAsync(tournamentId);
        }

        private async Task<IEnumerable<Deck>> GetDecksByTournamentIdAsync(int tournamentId, int? playerId = null)
        {
            var query = @"
                SELECT d.*, t.*, s.*,u.*, c.*, s2.*
                FROM decks d
                JOIN tournaments t ON d.tournament_id = t.tournament_id
                JOIN tournament_series ts ON t.tournament_id = ts.tournament_id 
                JOIN series s ON ts.series_id = s.series_id
                JOIN users u ON d.user_id = u.user_id
                JOIN deck_cards dc ON d.deck_id = dc.deck_id
                JOIN cards c ON dc.card_id = c.card_id
                JOIN card_series cs ON c.card_id = cs.card_id
                JOIN series s2 ON cs.series_id = s2.series_id
                WHERE d.tournament_id = @TournamentId
                AND t.is_canceled = 0";

            var parameters = new DynamicParameters();

            if (playerId.HasValue)
            {
                query += " AND d.user_id = @PlayerId";
                parameters.Add("PlayerId", playerId.Value);
            }

            await using var connection = CreateConnection();
            var deckDictionary = new Dictionary<int, Deck>();
            var cardDictionary = new Dictionary<int, Card>();

            var decks = await connection.QueryAsync<Deck, Tournament, Serie, User, Card, Serie, Deck>(
                query,
                (d, t, s, u, c, s2) =>
                {
                    if (!deckDictionary.TryGetValue(d.DeckId, out var deckEntry))
                    {
                        deckEntry = d;
                        deckEntry.Tournament = t;
                        deckEntry.Tournament.Series = [];
                        deckEntry.Player = u;
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

                        if (!deckEntry.Cards.Any(card => card.CardId == cardEntry.CardId))
                            deckEntry.Cards.Add(cardEntry);
                    }

                    return deckEntry;
                },
                new { PlayerId = playerId, TournamentId = tournamentId },
                splitOn: "tournament_id, series_id,user_id, card_id, series_id");

            return deckDictionary.Values;
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

        public async Task<bool> IsPlayerDisqualifiedAsync(int playerId, int tournamentId)
        {
            const string query = @"
                SELECT COUNT(*)
                FROM disqualifications
                WHERE user_id = @PlayerId
                AND tournament_id = @TournamentId;";

            await using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(query, new { PlayerId = playerId, TournamentId = tournamentId }) > 0;
        }

        public async Task<IEnumerable<Game>> GetTournamentGamesAsync(int tournamentId)
        {
            const string query = @"
                SELECT g.*, p1.*, p2.*, w.*
                FROM Games g
                LEFT JOIN Users p1 ON g.player1_id = p1.user_id
                LEFT JOIN Users p2 ON g.player2_id = p2.user_id
                LEFT JOIN Users w ON g.winner_id = w.user_id
                WHERE tournament_id = @TournamentId;";

            await using var connection = CreateConnection();
            var gamesDictionary = new Dictionary<int, Game>();

            var games = await connection.QueryAsync<Game, User, User, User, Game>(
                query,
                (g, p1, p2, w) =>
                {
                    if (!gamesDictionary.TryGetValue(g.GameId, out var gameEntry))
                    {
                        gameEntry = g;
                        gameEntry.Player1 = p1;
                        gameEntry.Player2 = p2;
                        gameEntry.Winner = w;
                        gamesDictionary.Add(gameEntry.GameId, gameEntry);
                    }
                    return gameEntry;
                },
                new { TournamentId = tournamentId },
                splitOn: "user_id, user_id, user_id");

            return gamesDictionary.Values;
        }

        public async Task<IEnumerable<Game>> GetTournamentGamesAsync(int? tournamentId = null, int? winnerId = null, int? loserId = null)
        {
            var queryBuilder = new StringBuilder(@"
                SELECT g.*, p1.*, p2.*, w.*
                FROM Games g
                LEFT JOIN Users p1 ON g.player1_id = p1.user_id
                LEFT JOIN Users p2 ON g.player2_id = p2.user_id
                LEFT JOIN Users w ON g.winner_id = w.user_id");

            var parameters = new DynamicParameters();
            var conditions = new List<string>();

            if (tournamentId.HasValue)
            {
                conditions.Add("g.tournament_id = @TournamentId");
                parameters.Add("TournamentId", tournamentId.Value);
            }

            if (winnerId.HasValue)
            {
                conditions.Add("g.winner_id = @WinnerId");
                parameters.Add("WinnerId", winnerId.Value);
            }

            if (loserId.HasValue)
            {
                conditions.Add(@"
                    (g.player1_id = @LoserId OR g.player2_id = @LoserId)
                    AND winner_id IS NOT NULL AND winner_id != @LoserId;");
                parameters.Add("LoserId", loserId.Value);
            }

            if (conditions.Any())
            {
                queryBuilder.Append(" WHERE ");
                queryBuilder.Append(string.Join(" AND ", conditions));
            }

            await using var connection = CreateConnection();
            var gamesDictionary = new Dictionary<int, Game>();

            var games = await connection.QueryAsync<Game, User, User, User, Game>(
                queryBuilder.ToString(),
                (g, p1, p2, w) =>
                {
                    if (!gamesDictionary.TryGetValue(g.GameId, out var gameEntry))
                    {
                        gameEntry = g;
                        gameEntry.Player1 = p1;
                        gameEntry.Player2 = p2;
                        gameEntry.Winner = w;
                        gamesDictionary.Add(gameEntry.GameId, gameEntry);
                    }
                    return gameEntry;
                },
                parameters,
                splitOn: "user_id, user_id, user_id");

            return gamesDictionary.Values;
        }


        public async Task<IEnumerable<Tournament>> GetTournamentsByUserIdAsync(int userId)
        {
            const string userQuery = "SELECT * FROM Users WHERE user_id = @UserId;";

            await using var connection = CreateConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User?>(userQuery, new { UserId = userId });

            if (user is null)
                return [];

            var tournamentQuery = user.Role switch
            {
                UserRole.Organizer => "SELECT * FROM Tournaments WHERE organizer_id = @UserId;",
                UserRole.Judge => @"
                    SELECT T.* FROM Tournaments T
                    JOIN tournament_judges TJ ON T.tournament_id = TJ.tournament_id
                    WHERE TJ.user_id = @UserId;",
                UserRole.Player => @"
                    SELECT * FROM Tournaments T
                    JOIN tournament_players TP ON T.tournament_id = TP.tournament_id            
                    WHERE TP.user_id = @UserId;",
                _ => throw new ArgumentException("Role not recognized")
            };

            return await connection.QueryAsync<Tournament>(tournamentQuery, new { UserId = userId });
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsByWinnerAsync(int userId)
        {
            const string userQuery = "SELECT * FROM Users WHERE user_id = @UserId;";

            await using var connection = CreateConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User?>(userQuery, new { UserId = userId });

            if (user?.Role is not UserRole.Player)
                throw new ArgumentException("User is not a player");

            const string tournamentQuery = @"
                SELECT * FROM Tournaments               
                WHERE winner_id = @UserId;";

            return await connection.QueryAsync<Tournament>(tournamentQuery, new { UserId = userId });
        }

        ////Funcion para obtener los juegos ganados por un jugador
        //public async Task<IEnumerable<Game>> GetWonGamesByPlayerIdAsync(int playerId)
        //{
        //    const string query = @"
        //        SELECT * FROM Games g
        //        LEFT JOIN Tournaments t ON G.tournament_id = t.tournament_id
        //        LEFT JOIN tournament_players AS tp ON t.tournament_id = tp.tournament_id
        //        LEFT JOIN users AS p ON tp.user_id = p.user_id
        //        LEFT JOIN users AS w ON t.winner_id = w.user_id
        //        WHERE winner_id = @PlayerId;";
        //    var parameters = new { PlayerId = playerId };
        //    await using var connection = CreateConnection();
        //    return await connection.QueryAsync<Game>(query, parameters);
        //}

        ////Funcion para obtener los juegos perdidos por un jugador
        //public async Task<IEnumerable<Game>> GetLostGamesByPlayerIdAsync(int playerId)
        //{
        //    const string query = @"
        //        SELECT * FROM Games G
        //        JOIN Tournaments T ON G.tournament_id = T.tournament_id
        //        WHERE (player1_id = @PlayerId OR player2_id = @PlayerId) AND winner_id IS NOT NULL AND winner_id != @PlayerId;";
        //    var parameters = new { PlayerId = playerId };
        //    await using var connection = CreateConnection();
        //    return await connection.QueryAsync<Game>(query, parameters);
        //}
    }
}
