using AutoMapper;
using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Domain.Exceptions;
using static TournamentSystem.Application.Helpers.TournamentServiceHelpers;

namespace TournamentSystem.Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ISerieRepository _serieRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IMapper _mapper;

        public TournamentService(
            ITournamentRepository tournamentRepository,
            ISerieRepository serieRepository,
            IUserRepository userRepository,
            ICardRepository cardRepository,
            IPlayerRepository playerRepository,
            IMapper mapper)
        {
            _tournamentRepository = tournamentRepository;
            _serieRepository = serieRepository;
            _userRepository = userRepository;
            _cardRepository = cardRepository;
            _playerRepository = playerRepository;
            _mapper = mapper;
        }

        public async Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId)
        {
            var minutesPerDay = CalculateTimePerDay(dto.StartDateTime, dto.EndDateTime).TotalMinutes;

            if (minutesPerDay < 30)
                throw new ValidationException("The tournament must have at least 30 minutes per day.");

            var series = await _serieRepository.GetSeriesByIdsAsync(dto.SeriesIds.ToArray());

            if (dto.SeriesIds.Count != series.Count)
                throw new NotFoundException("One or more series do not exist");

            var judgesExists = await _userRepository.UsersExistByIdsAndRoleAsync(dto.JudgesIds.ToArray(), UserRole.Judge);

            if (!judgesExists)
                throw new NotFoundException("One or more judges do not exist");

            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                CountryCode = dto.CountryCode,
                OrganizerId = oganizerId,
                Series = series,
                Judges = dto.JudgesIds.Select(j => new User { UserId = j }).ToList()
            };

            return await _tournamentRepository.CreateTournamentAsync(tournament);
        }

        public async Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto)
        {
            var existingTournament = (await _tournamentRepository.GetTournamentsAsync(dto.TournamentId)).FirstOrDefault();

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
            var tournament = (await _tournamentRepository.GetTournamentsAsync(tournamentId)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<TournamentAdminDto>(tournament)
                : _mapper.Map<TournamentPublicDto>(tournament);
        }

        public async Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(id: tournamentId, phase: TournamentPhase.Registration)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

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

            var cards = await _cardRepository.GetCardsByIdsAsync(cardsIds);
            var tournamentSeries = tournament.Series.Select(s => s.SeriesId);
            var allCardsValid = cards.All(c => c.Series.Any(cs => tournamentSeries.Contains(cs.SeriesId)));

            if (!allCardsValid)
                throw new ValidationException("One or more cards are not from the tournament series.");

            var player = await _tournamentRepository.RegisterPlayerAsync(tournamentId, playerId, cardsIds);
            tournament.Players.Add(player);

            if (tournament.Players.Count == maxPlayers)
                await _tournamentRepository.FinalizeRegistrationAndStartTournamentAsync(tournamentId, ScheduleGames(tournament));

            return true;
        }

        public async Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId, int organizerId)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(id: tournamentId, organizerId: organizerId)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            var judge = await _userRepository.GetUserByIdAsync(judgeId);

            if (judge is null || judge.Role != UserRole.Judge)
                throw new NotFoundException("Judge not found");

            if (tournament.Judges.Exists(j => j.UserId == judgeId))
                throw new ValidationException("The judge is already assigned to the tournament.");

            return await _tournamentRepository.AssignJudgeToTournamentAsync(tournamentId, judgeId);
        }

        public async Task<bool> AddSeriesToTournamentAsync(int tournamentId, int[] seriesIds, int organizerId)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(tournamentId)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            var series = await _serieRepository.GetSeriesByIdsAsync(seriesIds);

            if (series.Count != seriesIds.Length)
                throw new NotFoundException("One or more series do not exist");

            if (series.Any(s => tournament.Series.Exists(ts => ts.SeriesId == s.SeriesId)))
                throw new ValidationException("One or more series are already assigned to the tournament.");

            return await _tournamentRepository.AddSeriesToTournamentAsync(tournamentId, seriesIds) > 0;
        }

        public async Task<bool> FinalizeRegistrationAsync(int tournamentId, int organizerId)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(
                id: tournamentId,
                organizerId: organizerId,
                phase: TournamentPhase.Registration)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            var games = ScheduleGames(tournament);

            return await _tournamentRepository.FinalizeRegistrationAndStartTournamentAsync(tournamentId, games);
        }

        public async Task<bool> SetGameWinnerAsync(int tournamentId, int gameId, int judgeId, int winnerId)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(
                id: tournamentId,
                judgeIds: [judgeId],
                phase: TournamentPhase.Tournament)).FirstOrDefault();

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            var game = tournament.Games.FirstOrDefault(g => g.GameId == gameId);

            if (game is null)
                throw new NotFoundException("Game not found");

            // Para produccion tengo que descomentar
            //if (game.StartTime > DateTime.Now)
            //    throw new ValidationException("The game has not started yet.");

            if (game.Player1Id != winnerId && game.Player2Id != winnerId)
                throw new ValidationException("The winner is not a player in this game.");

            if (game.WinnerId != null)
                throw new ValidationException("Game already has a winner.");

            var result = await _tournamentRepository.SetGameWinnerAsync(gameId, winnerId);
            game.WinnerId = winnerId;

            if (tournament.Games.All(g => g.WinnerId != null))
                return await _tournamentRepository.FinalizeTournamentAsync(tournamentId);

            var hasUnfinishedGames = tournament.Games.Any(g => g.WinnerId == null && g.Player1Id != null && g.Player2Id != null);
            if (!hasUnfinishedGames)
            {
                var nextRoundGames = SetPlayersForNextRound(tournament);
                return await _tournamentRepository.AdvanceWinnersToNextRoundAsync(nextRoundGames);
            }

            return result;
        }

        public async Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int judgeId)
        {
            var tournament = (await _tournamentRepository.GetTournamentsAsync(
                id: tournamentId,
                judgeIds: [judgeId],
                phase: TournamentPhase.Tournament)).FirstOrDefault();

            if (tournament == null)
                throw new NotFoundException("Tournament not found");

            if (!tournament.Players.Exists(p => p.UserId == playerId))
                throw new ValidationException("The player is not registered for the tournament.");

            //throw new ValidationException("The player is already disqualified from the tournament.");

            return await _tournamentRepository.DisqualifyPlayerAsync(playerId, tournamentId, reason, judgeId);
        }

        public async Task<IEnumerable<BaseTournamentDto>> GetTournamentsAsync(TournamentPhase phase, UserRole userRole)
        {
            var tournaments = await _tournamentRepository.GetTournamentsAsync(phase: phase);

            if (tournaments is null)
                throw new NotFoundException("Tournaments not found");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<IEnumerable<TournamentAdminDto>>(tournaments)
                : _mapper.Map<IEnumerable<TournamentPublicDto>>(tournaments);
        }
    }
}
