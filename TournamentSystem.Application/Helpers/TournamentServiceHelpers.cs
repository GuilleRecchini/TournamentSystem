using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Helpers
{
    public static class TournamentServiceHelpers
    {
        private const int gameDurationInMinutes = 30;

        public static int CalculateMaxPlayers(Tournament tournament)
        {
            var minutesPerDay = CalculateTimePerDay(tournament.StartDateTime, tournament.EndDateTime).TotalMinutes;
            var totalDays = CalculateTotalDays(tournament.StartDateTime, tournament.EndDateTime);

            var tournamentPlayableMinutes = (minutesPerDay - minutesPerDay % gameDurationInMinutes) * totalDays;
            var maxGames = tournamentPlayableMinutes / gameDurationInMinutes;
            var possiblePlayers = maxGames + 1;

            var maxPlayers = 2;

            while (maxPlayers * 2 <= possiblePlayers)
            {
                maxPlayers *= 2;
            }

            return maxPlayers;
        }

        public static int CalculateGameRound(int totalRounds, int gameNumber)
        {
            return totalRounds - (int)Math.Log2(gameNumber);
        }

        public static TimeSpan CalculateTimePerDay(DateTime startDateTime, DateTime endDateTime)
        {
            var startHour = startDateTime.TimeOfDay;
            var endHour = endDateTime.TimeOfDay;

            if (endHour < startHour)
                endHour = endHour.Add(new TimeSpan(24, 0, 0));

            return endHour - startHour;
        }

        public static int CalculateTotalDays(DateTime startDateTime, DateTime endDateTime)
        {
            var daysDiff = endDateTime - startDateTime;

            var totalDays = 1;

            if (daysDiff.TotalDays > 1)
                totalDays = (int)Math.Ceiling(daysDiff.TotalDays);

            return totalDays;
        }

        public static List<Game> ScheduleGames(Tournament tournament)
        {
            var games = new List<Game>();

            var minutesPerDay = CalculateTimePerDay(tournament.StartDateTime, tournament.EndDateTime).TotalMinutes;
            var playableMinutesPerDay = minutesPerDay - minutesPerDay % gameDurationInMinutes;
            var maxGamesPerDay = (int)(playableMinutesPerDay / gameDurationInMinutes);
            var totalGames = tournament.Players.Count - 1;
            var totalRounds = (int)Math.Log(tournament.Players.Count, 2);

            var gameDateTime = tournament.StartDateTime;
            var shuffledPlayers = tournament.Players.Select(p => p.UserId).OrderBy(x => new Random().Next()).ToList();
            var gameNumber = totalGames;
            var playerIndex = 0;

            while (gameNumber > 0)
            {
                for (var i = 0; i < maxGamesPerDay && gameNumber > 0; i++)
                {
                    var roundNumber = totalRounds - (int)Math.Log(gameNumber, 2);
                    roundNumber = CalculateGameRound(totalRounds, gameNumber);

                    var game = new Game()
                    {
                        TournamentId = tournament.TournamentId,
                        StartTime = gameDateTime,
                    };

                    if (roundNumber == 1)
                    {
                        game.Player1Id = shuffledPlayers[playerIndex];
                        game.Player2Id = shuffledPlayers[playerIndex + 1];
                        playerIndex += 2;
                    }

                    games.Add(game);
                    gameDateTime = gameDateTime.AddMinutes(gameDurationInMinutes);
                    gameNumber--;
                }

                if (gameNumber > 0)
                {
                    gameDateTime = gameDateTime.AddMinutes(-playableMinutesPerDay).AddDays(1);
                }
            }

            return games;
        }

        // Metodos privados
        public static List<Game> SetPlayersForNextRound(Tournament tournament)
        {
            var remainingGames = tournament.Games.Count(g => g.WinnerId == null);
            var totalRounds = (int)Math.Log2(tournament.Players.Count);
            var currentRound = totalRounds - (int)Math.Log2(remainingGames) - 1;

            var previousRoundWinners = tournament.Games
                .Select((game, index) => new { Game = game, GameNumber = tournament.Games.Count - index })
                .Where(g => CalculateGameRound(totalRounds, g.GameNumber) == currentRound)
                .Select(g => g.Game.WinnerId)
                .ToList();

            var nextRoundGames = tournament.Games
                .Select((game, index) => new { Game = game, GameNumber = tournament.Games.Count - index })
                .Where(g => CalculateGameRound(totalRounds, g.GameNumber) == currentRound + 1)
                .Select(g => g.Game)
                .ToList();

            for (var i = 0; i < nextRoundGames.Count; i++)
            {
                var game = nextRoundGames[i];

                game.Player1Id = previousRoundWinners[2 * i];
                game.Player2Id = previousRoundWinners[2 * i + 1];
            }

            return nextRoundGames;
        }
    }
}