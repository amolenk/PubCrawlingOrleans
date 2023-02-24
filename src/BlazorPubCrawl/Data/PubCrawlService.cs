using System.Text;
using BlazorPubCrawl.Model;

namespace BlazorPubCrawl.Data;

public class PubCrawlService
{
    private readonly HttpClient _httpClient;

    public PubCrawlService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // TODO Implement 'proper' security.
        _httpClient.DefaultRequestHeaders.Add("CrawlerId", "amolenk");
    }

    public async Task<Venue[]> GetVenuesAsync()
    {
        return await _httpClient.GetFromJsonAsync<Venue[]>("/events/1/venues?t=" + DateTime.UtcNow.Ticks)
            ?? Array.Empty<Venue>();
    }

    public async Task<Venue> GetVenueAsync(string venueId)
    {
        return await _httpClient.GetFromJsonAsync<Venue>($"/events/1/venues/{venueId}")
            ?? new Venue(venueId, "Unknown", 0, 0, 0, false, new());
    }

    public async Task<Beer[]> GetBeerSelectionAsync()
    {
        return await _httpClient.GetFromJsonAsync<Beer[]>("/events/1/beers")
            ?? Array.Empty<Beer>();
    }

    public async Task<Dictionary<string, int>> GetBeerRatingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<Dictionary<string, int>>("/events/1/ratings")
            ?? new();
    }

    public async Task CheckInAsync(string venueId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/events/1/venues/{venueId}/crawlers");

        await _httpClient.SendAsync(request);
    }

    public async Task CheckOutAsync(string venueId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/events/1/venues/{venueId}/crawlers");

        await _httpClient.SendAsync(request);
    }

    public async Task RateBeerAsync(string beerId, int rating)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/events/1/ratings/{beerId}")
        {
            Content = new StringContent(rating.ToString(), Encoding.UTF8, "application/json")
        };

        await _httpClient.SendAsync(request);
    }
}
