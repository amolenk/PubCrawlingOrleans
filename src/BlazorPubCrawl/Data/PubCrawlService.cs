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

    public async Task<Venue> GetVenueAsync(string venueId)
    {
        return await _httpClient.GetFromJsonAsync<Venue>($"/events/1/venues/{venueId}")
            ?? new Venue(venueId, "Unknown", 0, 0, 0, new());
    }

    public async Task<Beer[]> GetBeerSelectionAsync()
    {
        return await _httpClient.GetFromJsonAsync<Beer[]>("/events/1/beers")
            ?? Array.Empty<Beer>();
    }

    public async Task<Dictionary<string, int>> GetBeerRatingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<Dictionary<string, int>>("/events/1/ratings/amolenk")
            ?? new();
    }
}
