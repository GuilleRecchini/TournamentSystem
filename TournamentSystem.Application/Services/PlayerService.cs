using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICardRepository _cardRepository;

        public PlayerService(IPlayerRepository playerRepository, IUserRepository userRepository, ICardRepository cardRepository)
        {
            _playerRepository = playerRepository;
            _userRepository = userRepository;
            _cardRepository = cardRepository;
        }

        public async Task<int> AddCardsToCollectionAsync(int[] cardsIds, int playerId)
        {
            var allCardsExist = await _cardRepository.DoAllCardsExistAsync(cardsIds);

            if (!allCardsExist)
                throw new NotFoundException("Some cards do not exist");

            var addedCount = await _playerRepository.AddCardsToCollectionAsync(cardsIds, playerId);

            if (addedCount == 0)
                throw new ValidationException("All cards were already in your collection.");

            return addedCount;
        }

        public async Task<IEnumerable<Card>> GetCardsByPlayerIdAsyncAsync(int playerId)
        {

            var user = await _userRepository.GetUserByIdAsync(playerId);

            if (user is null || user.Role != Domain.Enums.UserRole.Player)
                throw new NotFoundException("Player not found");

            return await _cardRepository.GetCardsByPlayerIdAsync(playerId);
        }
    }
}
