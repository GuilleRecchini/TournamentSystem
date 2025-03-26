using TournamentSystem.Application.Interfaces;
using TournamentSystem.DataAccess.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;

        public CardService(ICardRepository cardRepository)
        {
            _cardRepository = cardRepository;
        }

        public async Task<Card?> GetCardByIdAsync(int id)
        {
            var card = await _cardRepository.GetCardByIdAsync(id);

            return card ?? throw new NotFoundException("Card not found");
        }

        public async Task<IEnumerable<Card>?> GetCardsBySerieAsync(int id)
        {
            var cards = await _cardRepository.GetCardsBySerieAsync(id);

            return cards ?? throw new NotFoundException("Cards not found");
        }
    }
}
