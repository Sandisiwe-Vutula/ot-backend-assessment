namespace OT.Assessment.Services.Implementation
{
    public class RabbitMQPublisherService : IRabbitMQPublisherService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQPublisherService> _logger;
        private readonly string _queueName;

        public RabbitMQPublisherService(ILogger<RabbitMQPublisherService> logger, IOptions<RabbitMqSettings> options)
        {
            _logger = logger;
            var settings = options.Value;
            
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.Username,
                    Password = settings.Password,
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    AutomaticRecoveryEnabled = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _queueName = settings.QueueName;
                _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                _logger.LogInformation("Successfully connected to RabbitMQ at {Host}:{Port} and queue {QueueName}.", settings.Host, settings.Port, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish connection to RabbitMQ. Please verify the configuration and server status.");
                throw new ApplicationException("Failed to connect to RabbitMQ.", ex);
            }
        }

        public async Task PublishCasinoWagerAsync(CasinoWager wager)
        {
            await Task.Run(() =>
            {
                try
                {
                    var message = JsonSerializer.Serialize(wager);
                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;

                    _channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: properties, body: body);

                    _logger.LogInformation("Published wager {WagerId} to RabbitMQ queue.", wager.WagerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish wager {WagerId} to RabbitMQ queue.", wager.WagerId);
                    throw;
                }
            });
        }

        public void Dispose()
        {
            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ connection disposed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing RabbitMQ connection.");
            }
        }
    }
}
