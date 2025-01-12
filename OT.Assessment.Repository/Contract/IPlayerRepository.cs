namespace OT.Assessment.Repository.Contract
{
    public interface IPlayerRepository
    {
        Task AddCasinoWagerAsync(CasinoWager wager);
        Task<PagedResponse<CasinoWager>> GetCasinoWagersByPlayerAsync(Guid playerId, int page, int pageSize);
        Task<IEnumerable<dynamic>> GetTopSpendersAsync(int count, DateTime? startDate, DateTime? endDate);
    }
}
