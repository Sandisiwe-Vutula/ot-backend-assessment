namespace OT.Assessment.Repository.Implementation
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly SqlConnection _connection;

        public PlayerRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task AddCasinoWagerAsync(CasinoWager wager)
        {
            var procedure = PlayerStoredProcedures.InsertCasinoWager;
            await _connection.ExecuteAsync(procedure, wager, commandType: CommandType.StoredProcedure);
        }

        public async Task<PagedResponse<CasinoWager>> GetCasinoWagersByPlayerAsync(Guid playerId, int page, int pageSize)
        {
            var procedure = PlayerStoredProcedures.GetCasinoWagersByPlayer;
            var parameters = new { AccountId = playerId, Page = page, PageSize = pageSize };

            using var multi = await _connection.QueryMultipleAsync(procedure, parameters, commandType: CommandType.StoredProcedure);

            var wagers = await multi.ReadAsync<CasinoWager>();
            var metadata = await multi.ReadSingleAsync<PagedResponse<CasinoWager>>();

            return new PagedResponse<CasinoWager>
            {
                Data = wagers,
                Page = metadata.Page,
                PageSize = metadata.PageSize,
                Total = metadata.Total,
                TotalPages = metadata.TotalPages
            };
        }

        public async Task<IEnumerable<dynamic>> GetTopSpendersAsync(int count, DateTime? startDate, DateTime? endDate)
        {
            var procedure = PlayerStoredProcedures.GetTopSpenders;
            var parameters = new { Count = count, StartDate = startDate, EndDate = endDate };
            return await _connection.QueryAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
