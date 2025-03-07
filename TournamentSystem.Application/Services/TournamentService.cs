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
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(dto.TournamentId, new TournamentsFilter { IsCanceled = false });


            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            tournament.Name = dto.Name ?? tournament.Name;
            tournament.StartDateTime = dto.StartDateTime ?? tournament.StartDateTime;
            tournament.EndDateTime = dto.EndDateTime ?? tournament.EndDateTime;
            tournament.CountryCode = dto.CountryCode ?? tournament.CountryCode;
            tournament.WinnerId = dto.Winner ?? tournament.WinnerId;
            tournament.OrganizerId = dto.OrganizerId ?? tournament.OrganizerId;

            return await _tournamentRepository.UpdateTournamentAsync(tournament);
        }

        public async Task<BaseTournamentDto> GetTournamentByIdAsync(int tournamentId, UserRole userRole)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<TournamentAdminDto>(tournament)
                : _mapper.Map<TournamentPublicDto>(tournament);
        }

        public async Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    Phase = TournamentPhase.Registration,
                    IsCanceled = false
                });

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (tournament.IsCanceled)
                throw new ValidationException("The tournament is canceled and cannot be registered for.");

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

            await ValidatePlayerCardsAsync(playerId, cardsIds, tournament);

            var player = await _tournamentRepository.RegisterPlayerAsync(tournamentId, playerId, cardsIds);
            tournament.Players.Add(player);

            if (tournament.Players.Count == maxPlayers)
                await _tournamentRepository.FinalizeRegistrationAndStartTournamentAsync(tournamentId, ScheduleGames(tournament));

            return true;
        }

        public async Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId, int organizerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    OrganizerId = organizerId,
                    IsCanceled = false
                });

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
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    IsCanceled = false
                });

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
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    OrganizerId = organizerId,
                    Phase = TournamentPhase.Registration,
                    IsCanceled = false
                });

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            var games = ScheduleGames(tournament);

            return await _tournamentRepository.FinalizeRegistrationAndStartTournamentAsync(tournamentId, games);
        }

        public async Task<bool> SetGameWinnerAsync(int tournamentId, int gameId, int judgeId, int winnerId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    JudgeIds = [judgeId],
                    Phase = TournamentPhase.Registration,
                    IsCanceled = false
                });

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
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
                tournamentId,
                new TournamentsFilter
                {
                    JudgeIds = [judgeId],
                    Phase = TournamentPhase.Tournament,
                    IsCanceled = false
                });

            if (tournament == null)
                throw new NotFoundException("Tournament not found");

            if (!tournament.Players.Exists(p => p.UserId == playerId))
                throw new ValidationException("The player is not registered for the tournament.");

            //throw new ValidationException("The player is already disqualified from the tournament.");

            return await _tournamentRepository.DisqualifyPlayerAsync(playerId, tournamentId, reason, judgeId);
        }

        public async Task<IEnumerable<BaseTournamentDto>> GetTournamentsAsync(UserRole userRole)
        {
            var tournaments = await _tournamentRepository.GetTournamentsAsync();

            if (tournaments is null)
                throw new NotFoundException("Tournaments not found");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<IEnumerable<TournamentAdminDto>>(tournaments)
                : _mapper.Map<IEnumerable<TournamentPublicDto>>(tournaments);
        }

        public async Task<IEnumerable<BaseTournamentDto>> GetTournamentsByPhaseAsync(TournamentPhase phase, UserRole userRole)
        {
            var tournaments = await _tournamentRepository.GetTournamentsAsync(new TournamentsFilter { Phase = phase });

            if (tournaments is null)
                throw new NotFoundException("Tournaments not found");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<IEnumerable<TournamentAdminDto>>(tournaments)
                : _mapper.Map<IEnumerable<TournamentPublicDto>>(tournaments);
        }

        public async Task<int> AddCardsToDeckAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            var deck = await _tournamentRepository.GetPlayerDeckByTournamentIdAsync(tournamentId, playerId);

            if (deck is null)
                throw new NotFoundException("Player deck not found");

            if (deck.Tournament.StartDateTime - DateTime.Now <= TimeSpan.FromDays(1))
                throw new ValidationException("The player can only add cards to the deck at least 1 day before the tournament starts.");

            if (deck.Cards.Count + cardsIds.Length > 15)
                throw new ValidationException("The player can only have up to 15 cards in the deck.");

            await ValidatePlayerCardsAsync(playerId, cardsIds, deck.Tournament);
            return await _tournamentRepository.AddCardsToDeckAsync(deck.DeckId, cardsIds);
        }

        public async Task<int> RemoveCardsFromDeckAsync(int tournamentId, int playerId, int[] cardsIds)
        {
            var deck = await _tournamentRepository.GetPlayerDeckByTournamentIdAsync(tournamentId, playerId);
            if (deck is null)
                throw new NotFoundException("Player deck not found");

            if (deck.Tournament.StartDateTime - DateTime.Now <= TimeSpan.FromDays(1))
                throw new ValidationException("The player can only remove cards from the deck at least 1 day before the tournament starts.");

            return await _tournamentRepository.RemoveCardsFromDeckAsync(deck.DeckId, cardsIds);
        }


        private async Task ValidatePlayerCardsAsync(int playerId, int[] cardsIds, Tournament tournament)
        {
            var playerCards = await _playerRepository.GetCardsByPlayerIdAsync(playerId);
            var areCardsOwnedByPlayer = cardsIds.All(cardId => playerCards.Any(c => c.CardId == cardId));

            if (!areCardsOwnedByPlayer)
                throw new ValidationException("One or more cards are not owned by the player.");

            var cards = await _cardRepository.GetCardsByIdsAsync(cardsIds);
            var tournamentSeries = tournament.Series.Select(s => s.SeriesId);
            var allCardsValid = cards.All(c => c.Series.Any(cs => tournamentSeries.Contains(cs.SeriesId)));

            if (!allCardsValid)
                throw new ValidationException("One or more cards are not from the tournament series.");
        }

        public async Task<DeckDto> GetDeckAsync(int playerId, int tournamentId)
        {
            var deck = await _tournamentRepository.GetPlayerDeckByTournamentIdAsync(tournamentId, playerId);

            return new DeckDto
            {
                DeckId = deck.DeckId,
                PlayerId = playerId,
                TournamentId = tournamentId,
                Cards = deck.Cards.ConvertAll(c => new Card
                {
                    CardId = c.CardId,
                    Name = c.Name,
                    Series = c.Series
                })
            };
        }

        public async Task<IEnumerable<DeckDto>> GetTournamentDecksAsyncAsync(int tournamentId)
        {
            var decks = await _tournamentRepository.GetTournamentDecksAsync(tournamentId);

            return decks.Select(d => new DeckDto
            {
                DeckId = d.DeckId,
                PlayerId = d.Player.UserId,
                TournamentId = d.Tournament.TournamentId,
                Cards = d.Cards.ConvertAll(c => new Card
                {
                    CardId = c.CardId,
                    Name = c.Name,
                    Series = c.Series
                })
            });
        }

        public async Task<bool> CancelTournamentAsync(int tournamentId, int userId, UserRole userRole)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(
             tournamentId,
             new TournamentsFilter
             {
                 IsCanceled = false
             });

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            if (userRole == UserRole.Organizer && tournament.OrganizerId != userId)
                throw new ValidationException("You are not the organizer of this tournament.");

            return await _tournamentRepository.CancelTournamentAsync(tournamentId);
        }
    }
}
