
HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://localhost:5001")
};


await RunCrawlerActions(1000, HttpMethod.Post);
// await RunCrawlerActions(1000, HttpMethod.Delete);

async Task RunCrawlerActions(int count, HttpMethod method)
{
    await Parallel.ForEachAsync(Enumerable.Range(0, count), async (i, _) =>
    {
        var request = new HttpRequestMessage(method, $"/events/1/venues/herberg-jan/crawlers/crawler{i}");

        await client.SendAsync(request);
    });
}
