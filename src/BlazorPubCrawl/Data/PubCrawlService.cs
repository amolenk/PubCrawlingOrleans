using BlazorPubCrawl.Model;

namespace BlazorPubCrawl.Data;

public class PubCrawlService
{
    private readonly HttpClient _httpClient;

    public PubCrawlService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Venue[]> GetVenuesAsync()
    {
        return await _httpClient.GetFromJsonAsync<Venue[]>("/events/1/venues")
            ?? Array.Empty<Venue>();
    }
}
