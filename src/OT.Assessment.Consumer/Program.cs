using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OT.Assessment.Consumer.Services;
using OT.Assessment.Repository.Implementation;

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
        services.AddTransient<SqlConnection>(provider =>
        {
            var connectionString = context.Configuration.GetConnectionString("DatabaseConnection");
            return new SqlConnection(connectionString);
        });
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddHostedService<RabbitMqConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

await host.RunAsync();

logger.LogInformation("Application ended {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);