namespace OT.Assessment.Repository.Implementation
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<PlayerRepository> _logger;

        public PlayerRepository(IDbConnection connection, ILogger<PlayerRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }


        public async Task AddCasinoWagerAsync(CasinoWager wager)
        {
            var procedure = PlayerStoredProcedures.InsertCasinoWager;

            try
            {
                _logger.LogInformation("Adding casino wager {WagerId}.", wager.WagerId);
                await _connection.ExecuteAsync(procedure, wager, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("Casino wager {WagerId} successfully added.", wager.WagerId);
            }
            catch (SqlException ex) when (ex.Number == 2627) // This is for unique constraint violation
            {
                _logger.LogWarning("Duplicate wager detected. Wager {WagerId} was not added.", wager.WagerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding casino wager {WagerId}.", wager.WagerId);
                throw;
            }
        }

        public async Task<PagedResponse<CasinoWager>> GetCasinoWagersByPlayerAsync(Guid playerId, int page, int pageSize)
        {
            var procedure = PlayerStoredProcedures.GetCasinoWagersByPlayer;
            var parameters = new { AccountId = playerId, Page = page, PageSize = pageSize };

            try
            {
                _logger.LogInformation("Getting casino wagers for player {PlayerId} (page {Page}, size {PageSize})", playerId, page, pageSize);
                
                using var multiWagers = await _connection.QueryMultipleAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
                var wagers = await multiWagers.ReadAsync<CasinoWager>();
                var metadata = await multiWagers.ReadSingleAsync<PagedResponse<CasinoWager>>();
               
                _logger.LogInformation("Retrieved {Count} casino wagers for player {PlayerId}", wagers.Count(), playerId);
                return new PagedResponse<CasinoWager>
                {
                    Data = wagers,
                    Page = metadata.Page,
                    PageSize = metadata.PageSize,
                    Total = metadata.Total,
                    TotalPages = metadata.TotalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting casino wagers for player {PlayerId}", playerId);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetTopSpendersAsync(int count, DateTime? startDate, DateTime? endDate)
        {
            var procedure = PlayerStoredProcedures.GetTopSpenders;
            var parameters = new { Count = count, StartDate = startDate, EndDate = endDate };

            try
            {
                _logger.LogInformation("Getting top {Count} spenders between {StartDate} and {EndDate}.", count, startDate, endDate);

                var spenders = await _connection.QueryAsync(procedure, parameters, commandType: CommandType.StoredProcedure);

                _logger.LogInformation("Successfully fetched top {Count} spenders.", count);
                return spenders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting top spenders.");
                throw;
            }
        }
    }
}