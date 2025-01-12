namespace OT.Assessment.Consumer.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private readonly IPlayerRepository _repository;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;

        public RabbitMqConsumerService(ILogger<RabbitMqConsumerService> logger, IPlayerRepository repository,IOptions<RabbitMqSettings> options)
        {
            _logger = logger;
            _repository = repository;
            var settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _queueName = settings.QueueName;

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ Consumer initialized and connected to queue {QueueName}.", _queueName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var wager = JsonSerializer.Deserialize<CasinoWager>(message);

                    if (wager != null)
                    {
                        await ProcessMessageAsync(wager);
                    }
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {Message}", message);
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue the message
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(CasinoWager wager)
        {
            try
            {
                await _repository.AddCasinoWagerAsync(wager);
                _logger.LogInformation("Successfully processed wager {WagerId}.", wager.WagerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process wager {WagerId}.", wager.WagerId);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
