using Microsoft.Extensions.Logging;

namespace OT.Assessment.Repository.Implementation
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly SqlConnection _connection;
        private readonly ILogger<PlayerRepository> _logger;

        public PlayerRepository(SqlConnection connection, ILogger<PlayerRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task AddCasinoWagerAsync(CasinoWager wager)
        {
            try
            {
                var procedure = PlayerStoredProcedures.InsertCasinoWager;
                _logger.LogInformation("Adding casino wager {WagerId}", wager.WagerId);
                await _connection.ExecuteAsync(procedure, wager, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("Casino wager {WagerId} added successfully", wager.WagerId);
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
            {
                _logger.LogWarning("Duplicate wager {WagerId} detected. Skipping.", wager.WagerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding casino wager {WagerId}", wager.WagerId);
                throw;
            }
        }

        public async Task<PagedResponse<CasinoWager>> GetCasinoWagersByPlayerAsync(Guid playerId, int page, int pageSize)
        {
            try
            {
                var procedure = PlayerStoredProcedures.GetCasinoWagersByPlayer;
                var parameters = new { AccountId = playerId, Page = page, PageSize = pageSize };
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
            try
            {
                var procedure = PlayerStoredProcedures.GetTopSpenders;
                var parameters = new { Count = count, StartDate = startDate, EndDate = endDate };
                _logger.LogInformation("Getting top {Count} spenders (start date: {StartDate}, end date: {EndDate})", count, startDate, endDate);
                
                var result = await _connection.QueryAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("Retrieved {Count} top spenders", result.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top spenders");
                throw;
            }
        }
    }
}