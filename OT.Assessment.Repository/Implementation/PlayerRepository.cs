namespace OT.Assessment.Repository.Implementation
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<PlayerRepository> _logger;
        private readonly IDistributedCache _cache;

        public PlayerRepository(IDistributedCache cache, IDbConnection connection, ILogger<PlayerRepository> logger)
        {
            _connection = connection;
            _logger = logger;
            _cache = cache;
        }

        public async Task AddCasinoWagerAsync(CasinoWager wager)
        {
            var procedure = PlayerStoredProcedures.InsertCasinoWager;

            try
            {
                _logger.LogInformation("Adding casino wager {WagerId}.", wager.WagerId);
                await _connection.ExecuteAsync(procedure, wager, commandType: CommandType.StoredProcedure);

                _logger.LogInformation("Casino wager {WagerId} successfully added.", wager.WagerId);

                // Cache invalidation
                var cacheKeyPattern = $"PlayerWagers_{wager.WagerId}_Page*";
                await InvalidatePlayerWagerCacheAsync(wager.WagerId, cacheKeyPattern);
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
            var cachedResponse = await GetCachedPlayerWagersAsync(playerId, page, pageSize);
            var procedure = PlayerStoredProcedures.GetCasinoWagersByPlayer;
            var parameters = new { AccountId = playerId, Page = page, PageSize = pageSize };

            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            try
            {
                _logger.LogInformation("Getting casino wagers for player {PlayerId} (page {Page}, size {PageSize})", playerId, page, pageSize);

                using var multiWagers = await _connection.QueryMultipleAsync(procedure, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 60);
                var wagers = await multiWagers.ReadAsync<CasinoWager>();
                var metadata = await multiWagers.ReadSingleAsync<PagedResponse<CasinoWager>>();

                var response = new PagedResponse<CasinoWager>
                {
                    Data = wagers,
                    Page = metadata.Page,
                    PageSize = metadata.PageSize,
                    Total = metadata.Total,
                    TotalPages = metadata.TotalPages
                };

                var cacheKey = $"PlayerWagers_{playerId}_Page{page}_Size{pageSize}";
                try
                {
                    var serializedData = JsonSerializer.Serialize(response);
                    await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    _logger.LogInformation("Cached player {PlayerId} wagers (page {Page}, size {PageSize})", playerId, page, pageSize);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error caching data for player {PlayerId}", playerId);
                }

                _logger.LogInformation("Retrieved {Count} casino wagers for player {PlayerId}", wagers.Count(), playerId);
                return response;
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

        private async Task<PagedResponse<CasinoWager>?> GetCachedPlayerWagersAsync(Guid playerId, int page, int pageSize)
        {
            var cacheKey = $"PlayerWagers_{playerId}_Page{page}_Size{pageSize}";
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var cachedResponse = JsonSerializer.Deserialize<PagedResponse<CasinoWager>>(cachedData);
                    if (cachedResponse != null)
                    {
                        _logger.LogInformation("Cache hit for player {PlayerId} (page {Page}, size {PageSize})", playerId, page, pageSize);
                        return cachedResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache for player {PlayerId}", playerId);
            }
            return null;
        }

        private async Task InvalidatePlayerWagerCacheAsync(Guid playerId, string cacheKeyPattern)
        {
            try
            {
                _logger.LogInformation("Invalidating cache for player {PlayerId} with pattern {CacheKeyPattern}.", playerId, cacheKeyPattern);

                // Retrieving all cache keys matching the pattern
                var redisKeys = _cache as IConnectionMultiplexer;
                if (redisKeys != null)
                {
                    var server = redisKeys.GetServer(redisKeys.GetEndPoints().First());
                    var keys = server.Keys(pattern: cacheKeyPattern);

                    foreach (var key in keys)
                    {
                        await _cache.RemoveAsync(key);
                        _logger.LogInformation("Cache key {CacheKey} invalidated.", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for player {PlayerId}.", playerId);
            }
        }

    }
}