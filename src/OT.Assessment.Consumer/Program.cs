var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    })
    .ConfigureServices((context, services) =>
    {
        //configure services
        services.Configure<RabbitMqSettings>(context.Configuration.GetSection("RabbitMQ"));
        services.AddTransient<IDbConnection>(provider =>
        {
            var connectionString = context.Configuration.GetConnectionString("DatabaseConnection");
            return new SqlConnection(connectionString);
        });
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = context.Configuration.GetSection("Redis")["ConnectionString"];
            options.InstanceName = context.Configuration.GetSection("Redis")["InstanceName"];
        });
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IRabbitMQPublisherService, RabbitMQPublisherService>();
        services.AddHostedService<RabbitMqConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

await host.RunAsync();

logger.LogInformation("Application ended {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);