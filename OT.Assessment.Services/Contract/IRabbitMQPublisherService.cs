namespace OT.Assessment.Services.Contract
{
    public interface IRabbitMQPublisherService
    {
        Task PublishCasinoWagerAsync(CasinoWager wager);
    }
}
