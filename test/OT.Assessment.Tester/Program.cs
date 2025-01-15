// See https://aka.ms/new-console-template for more information

var bg = new BogusGenerator();
var total = bg.Generate();

var postWagersScenario = Scenario.Create("post_casino_wagers", async context =>
{
    var body = JsonSerializer.Serialize(total[(int)context.InvocationNumber]);
    using var httpClient = new HttpClient();
    var request = Http.CreateRequest("POST", "https://localhost:7120/api/Player/CasinoWager")
        .WithHeader("Accept", "application/json")
        .WithBody(new StringContent(body, Encoding.UTF8, "application/json"));

    var response = await Http.Send(httpClient, request);

    if (response.StatusCode == "OK") return Response.Ok();
    return Response.Fail(body, response.StatusCode, response.Message, response.SizeBytes);
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.IterationsForInject(rate: 500,
        interval: TimeSpan.FromSeconds(2),
        iterations: 7000)
);

var getTopSpendersScenario = Scenario.Create("get_top_spenders", async context =>
{
    var url = "https://localhost:7120/api/player/topSpenders?count=10";
    using var httpClient = new HttpClient();
    var request = Http.CreateRequest("GET", url).WithHeader("Accept", "application/json");

    var response = await Http.Send(httpClient, request);

    if (response.StatusCode == "OK") return Response.Ok();
    return Response.Fail(response.Message, response.StatusCode, response.SizeBytes);
});

var getCasinoWagersScenario = Scenario.Create("get_casino_wagers", async context =>
{
    var playerId = "AF796258-352B-A965-FAA9-2435C8C9C979";
    var page = 1;
    var pageSize = 10;
    var url = $"https://localhost:7120/api/player/{playerId}/wagers?page={page}&pageSize={pageSize}";

    using var httpClient = new HttpClient();
    var request = Http.CreateRequest("GET", url).WithHeader("Accept", "application/json");

    var response = await Http.Send(httpClient, request);

    if (response.StatusCode == "OK") return Response.Ok();
    return Response.Fail(response.Message, response.StatusCode, response.SizeBytes);
});

var cacheHitScenario = Scenario.Create("test_cache_hit", async context =>
{
    var playerId = "AF796258-352B-A965-FAA9-2435C8C9C979";
    var page = 1;
    var pageSize = 10;
    var url = $"https://localhost:7120/api/player/{playerId}/wagers?page={page}&pageSize={pageSize}";
    using var httpClient = new HttpClient();
    var request = Http.CreateRequest("GET", url).WithHeader("Accept", "application/json");
    var response = await Http.Send(httpClient, request);
    if (response.StatusCode == "OK") return Response.Ok();
    return Response.Fail(response.Message, response.StatusCode, response.SizeBytes);
});

// Register and running all scenarios
NBomberRunner
    .RegisterScenarios(postWagersScenario, getTopSpendersScenario, getCasinoWagersScenario, cacheHitScenario)
    .WithWorkerPlugins(new HttpMetricsPlugin(new[] { HttpVersion.Version1 }))
    .WithoutReports()
    .Run();
