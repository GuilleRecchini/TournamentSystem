using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ISerieRepository _serieRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IPlayerRepository _playerRepository;
        private const int gameDurationInMinutes = 30;

        public TournamentService(
            ITournamentRepository tournamentRepository,
            ISerieRepository serieRepository,
            IUserRepository userRepository,
            ICardRepository cardRepository,
            IPlayerRepository playerRepository)
        {
            _tournamentRepository = tournamentRepository;
            _serieRepository = serieRepository;
            _userRepository = userRepository;
            _cardRepository = cardRepository;
            _playerRepository = playerRepository;
        }

        public async Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId)
        {
            var minutesPerDay = CalculateTimePerDay(dto.StartDateTime, dto.EndDateTime).TotalMinutes;

            if (minutesPerDay < 30)
                throw new ValidationException("The tournament must have at least 30 minutes per day.");

            var series = await _serieRepository.GetSeriesAsync(dto.SeriesIds.ToArray());

            if (dto.SeriesIds.Count != series.Count)
                throw new NotFoundException("One or more series do not exist");

            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                CountryCode = dto.CountryCode,
                OrganizerId = oganizerId,
                Series = series
            };

            return await _tournamentRepository.CreateTournamentAsync(tournament);
        }

        public async Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto)
        {
            var existingTournament = await _tournamentRepository.GetTournamentByIdAsync(dto.TournamentId);

            if (existingTournament is null)
                throw new NotFoundException("Tournament not found");

            existingTournament.Name = dto.Name ?? existingTournament.Name;
            existingTournament.StartDateTime = dto.StartDateTime ?? existingTournament.StartDateTime;
            existingTournament.EndDateTime = dto.EndDateTime ?? existingTournament.EndDateTime;
            existingTournament.CountryCode = dto.CountryCode ?? existingTournament.CountryCode;
            existingTournament.Winner = dto.Winner ?? existingTournament.Winner;
            existingTournament.OrganizerId = dto.OrganizerId ?? existingTournament.OrganizerId;

            return await _tournamentRepository.UpdateTournamentAsync(existingTournament);
        }

        public async Task<BaseTournamentDto> GetTournamentByIdAsync(int tournamentId, UserRole userRole)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (userRole == UserRole.Administrator || userRole == UserRole.Organizer)
            {
                return new TournamentAdminDto
                {
                    TournamentId = tournament.TournamentId,
                    Name = tournament.Name,
                    StartDateTime = tournament.StartDateTime,
                    EndDateTime = tournament.EndDateTime,
                    CountryCode = tournament.CountryCode,
                    Winner = tournament.Winner,
                    MaxPlayers = CalculateMaxPlayers(tournament),
                    Series = tournament.Series,
                    Players = tournament.Players.ConvertAll(p => new UserForAdminsDto
                    {
                        UserId = p.UserId,
                        Name = p.Name,
                        Alias = p.Alias,
                        Email = p.Email,
                        AvatarUrl = p.AvatarUrl,
                        CountryCode = p.CountryCode,
                        Role = p.Role
                    }),
                    Judges = tournament.Judges.ConvertAll(j => new UserForAdminsDto
                    {
                        UserId = j.UserId,
                        Name = j.Name,
                        Alias = j.Alias,
                        Email = j.Email,
                        AvatarUrl = j.AvatarUrl,
                        CountryCode = j.CountryCode,
                        Role = j.Role
                    }),
                    Organizer = new UserForAdminsDto
                    {
                        UserId = tournament.Organizer.UserId,
                        Name = tournament.Organizer.Name,
                        Alias = tournament.Organizer.Alias,
                        Email = tournament.Organizer.Email,
                        AvatarUrl = tournament.Organizer.AvatarUrl,
                        CountryCode = tournament.Organizer.CountryCode,
                        Role = tournament.Organizer.Role
                    }
                };
            }

            return new TournamentPublicDto
            {
                TournamentId = tournament.TournamentId,
                Name = tournament.Name,
                StartDateTime = tournament.StartDateTime,
                EndDateTime = tournament.EndDateTime,
                CountryCode = tournament.CountryCode,
                Winner = tournament.Winner,
                MaxPlayers = CalculateMaxPlayers(tournament),
                Series = tournament.Series,
                Players = tournament.Players.ConvertAll(p => new BaseUserDto
                {
                    Alias = p.Alias,
                    AvatarUrl = p.AvatarUrl,
                    CountryCode = p.CountryCode
                }),
                Judges = tournament.Judges.ConvertAll(j => new BaseUserDto
                {
                    Alias = j.Alias,
                    AvatarUrl = j.AvatarUrl,
                    CountryCode = j.CountryCode,
                }),
                Organizer = new BaseUserDto
                {
                    Alias = tournament.Organizer.Alias,
                    AvatarUrl = tournament.Organizer.AvatarUrl,
                    CountryCode = tournament.Organizer.CountryCode,
                }
            };
        }

        public async Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.Phase != TournamentPhase.Registration)
                throw new ValidationException("The tournament is not in the registration phase.");

            if (tournament.Players.Exists(p => p.UserId == playerId))
                throw new ValidationException("The player is already registered for the tournament.");

            var maxPlayers = CalculateMaxPlayers(tournament);
            var currentPlayers = tournament.Players.Count;

            if (currentPlayers == maxPlayers)
                throw new ValidationException("The tournament has reached its maximum capacity of players.");

            if (cardsIds.Length > 15)
                throw new ValidationException("The player can only register up to 15 cards.");

            if (cardsIds.Length != cardsIds.Distinct().Count())
                throw new ValidationException("The player can only register unique cards.");

            var playerCards = await _playerRepository.GetCardsByPlayerIdAsync(playerId);
            var areCardsOwnedByPlayer = cardsIds.All(cardId => playerCards.Any(c => c.CardId == cardId));

            if (!areCardsOwnedByPlayer)
                throw new ValidationException("One or more cards are not owned by the player.");

            var cards = await _cardRepository.GetCardsByIdsWithSeriesAsync(cardsIds);
            var tournamentSeries = tournament.Series.Select(s => s.SeriesId);
            var allCardsValid = cards.All(c => c.Series.Any(cs => tournamentSeries.Contains(cs.SeriesId)));

            if (!allCardsValid)
                throw new ValidationException("One or more cards are not from the tournament series.");

            return await _tournamentRepository.RegisterPlayerAsync(tournamentId, playerId, cardsIds);
        }

        public async Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId, int organizerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.OrganizerId != organizerId)
                throw new ValidationException("The organizer is not allowed to assign judges to this tournament.");

            var judge = await _userRepository.GetUserByIdAsync(judgeId);

            if (judge is null || judge.Role != UserRole.Judge)
                throw new NotFoundException("Judge not found");

            if (tournament.Judges.Exists(j => j.UserId == judgeId))
                throw new ValidationException("The judge is already assigned to the tournament.");

            return await _tournamentRepository.AssignJudgeToTournamentAsync(tournamentId, judgeId);
        }

        public async Task<bool> AddSeriesToTournamentAsync(int tournamentId, int[] seriesIds, int organizerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.OrganizerId != organizerId)
                throw new ValidationException("The organizer is not allowed to assign judges to this tournament.");

            var series = await _serieRepository.GetSeriesAsync(seriesIds);

            if (series.Count != seriesIds.Length)
                throw new NotFoundException("One or more series do not exist");

            if (series.Any(s => tournament.Series.Exists(ts => ts.SeriesId == s.SeriesId)))
                throw new ValidationException("One or more series are already assigned to the tournament.");

            return await _tournamentRepository.AddSeriesToTournamentAsync(tournamentId, seriesIds) > 0;
        }

        public async Task<bool> FinalizeRegistrationAsync(int tournamentId, int organizerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.OrganizerId != organizerId)
                throw new ValidationException("The organizer is not allowed to assign judges to this tournament.");

            if (tournament.Phase != TournamentPhase.Registration)
                throw new ValidationException("The tournament is not in the registration phase.");

            var games = ScheduleGames(tournament);

            return await _tournamentRepository.FinalizeRegistrationAndStartTournamentAsync(tournamentId, games);
        }

        public async Task<bool> SetGameWinnerAsync(int tournamentId, int gameId, int judgeId, int winnerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.Phase != TournamentPhase.Tournament)
                throw new ValidationException("The tournament is not in the tournament phase.");

            if (!tournament.Judges.Exists(j => j.UserId == judgeId))
                throw new ValidationException("The judge is not assigned to this tournament.");

            var game = tournament.Games.FirstOrDefault(g => g.GameId == gameId);

            if (game is null)
                throw new NotFoundException("Game not found");

            if (game.StartTime > DateTime.Now)
                throw new ValidationException("The game has not started yet.");

            if (game.Player1Id != winnerId && game.Player2Id != winnerId)
                throw new ValidationException("The winner is not a player in this game.");

            if (game.WinnerId != null)
                throw new ValidationException("Game already has a winner.");

            return await _tournamentRepository.SetGameWinnerAsync(gameId, winnerId);
        }

        public async Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int judgeId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament == null)
                throw new NotFoundException("Tournament not found");

            if (tournament.Phase != TournamentPhase.Tournament)
                throw new ValidationException("The tournament is not in the tournament phase.");

            if (!tournament.Judges.Exists(j => j.UserId == judgeId))
                throw new ValidationException("The judge is not assigned to this tournament.");

            if (!tournament.Players.Exists(p => p.UserId == playerId))
                throw new ValidationException("The player is not registered for the tournament.");

            //throw new ValidationException("The player is already disqualified from the tournament.");

            return await _tournamentRepository.DisqualifyPlayerAsync(playerId, tournamentId, reason, judgeId);
        }

        // Metodos privados para calculos
        private static TimeSpan CalculateTimePerDay(DateTime startDateTime, DateTime endDateTime)
        {
            var startHour = startDateTime.TimeOfDay;
            var endHour = endDateTime.TimeOfDay;

            if (endHour < startHour)
                endHour = endHour.Add(new TimeSpan(24, 0, 0));

            return endHour - startHour;
        }

        private static int CalculateTotalDays(DateTime startDateTime, DateTime endDateTime)
        {
            var daysDiff = endDateTime - startDateTime;

            var totalDays = 1;

            if (daysDiff.TotalDays > 1)
                totalDays = (int)Math.Ceiling(daysDiff.TotalDays);

            return totalDays;
        }

        private static int CalculateMaxPlayers(Tournament tournament)
        {
            var minutesPerDay = CalculateTimePerDay(tournament.StartDateTime, tournament.EndDateTime).TotalMinutes;
            var totalDays = CalculateTotalDays(tournament.StartDateTime, tournament.EndDateTime);

            var tournamentPlayableMinutes = (minutesPerDay - (minutesPerDay % gameDurationInMinutes)) * totalDays;
            var maxGames = tournamentPlayableMinutes / gameDurationInMinutes;
            var possiblePlayers = maxGames + 1;

            var maxPlayers = 2;

            while (maxPlayers * 2 <= possiblePlayers)
            {
                maxPlayers *= 2;
            }

            return maxPlayers;
        }

        private static List<Game> ScheduleGames(Tournament tournament)
        {
            var games = new List<Game>();

            var minutesPerDay = CalculateTimePerDay(tournament.StartDateTime, tournament.EndDateTime).TotalMinutes;
            var playableMinutesPerDay = minutesPerDay - (minutesPerDay % gameDurationInMinutes);
            var maxGamesPerDay = (int)(playableMinutesPerDay / gameDurationInMinutes);
            var totalGames = tournament.Players.Count - 1;
            var numRounds = (int)Math.Log(tournament.Players.Count, 2);

            var gameDateTime = tournament.StartDateTime;
            var shuffledPlayers = tournament.Players.Select(p => p.UserId).OrderBy(x => new Random().Next()).ToList();
            var gamesRemaining = totalGames;
            var playerIndex = 0;

            while (gamesRemaining > 0)
            {
                for (var i = 0; i < maxGamesPerDay && gamesRemaining > 0; i++)
                {
                    var roundNumber = numRounds - (int)Math.Log(gamesRemaining, 2);

                    var game = new Game()
                    {
                        TournamentId = tournament.TournamentId,
                        RoundNumber = roundNumber,
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
                    gamesRemaining--;
                }

                if (gamesRemaining > 0)
                {
                    gameDateTime = gameDateTime.AddMinutes(-playableMinutesPerDay).AddDays(1);
                }
            }

            return games;
        }
    }
}
