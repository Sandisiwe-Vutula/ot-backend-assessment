namespace OT.Assessment.Services.Implementation
{
    public class PlayerService : IPlayerService
    {
        private readonly IRabbitMQPublisherService _publisherService;
        private readonly IPlayerRepository _repository;

        public PlayerService(IRabbitMQPublisherService publisherService, IPlayerRepository repository)
        {
            _publisherService = publisherService;
            _repository = repository;
        }

        public async Task AddCasinoWagerAsync(CasinoWager wager)
        {
            await _publisherService.PublishCasinoWagerAsync(wager);
        }

        public async Task<PagedResponse<CasinoWager>> GetCasinoWagersByPlayerAsync(Guid playerId, int page, int pageSize)
        {
            return await _repository.GetCasinoWagersByPlayerAsync(playerId, page, pageSize);
        }

        public async Task<IEnumerable<dynamic>> GetTopSpendersAsync(int count, DateTime? startDate, DateTime? endDate)
        {
            return await _repository.GetTopSpendersAsync(count, startDate, endDate);
        }
    }
}
