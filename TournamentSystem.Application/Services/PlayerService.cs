using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;

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

        public async Task<bool> AddCardsToCollectionAsync(int[] cardsIds, int playerId)
        {
            var player = await _userRepository.GetUserByIdAsync(playerId);
            if (player is null)
                return false;

            var allCardsExist = await _cardRepository.DoAllCardsExistAsync(cardsIds);
            if (!allCardsExist)
                return false;

            return await _playerRepository.AddCardsToCollectionAsync(cardsIds, playerId);
        }

        public async Task<IEnumerable<Card>> GetCardsByPlayerIdAsyncAsync(int playerId)
        {
            return await _playerRepository.GetCardsByPlayerIdAsync(playerId);
        }
    }
}
