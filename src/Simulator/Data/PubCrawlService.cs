using System.Net.Http.Json;
using System.Text;

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
            ?? new();
    }

    public async Task CheckInAsync(string venueId, string crawlerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/events/1/venues/{venueId}/crawlers");
        request.Headers.Add("CrawlerId", crawlerId);

        await _httpClient.SendAsync(request);
    }

    public async Task RateBeerAsync(string beerId, int rating, string crawlerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/events/1/beers/{beerId}/ratings")
        {
            Content = new StringContent(rating.ToString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("CrawlerId", crawlerId);

        await _httpClient.SendAsync(request);
    }
}
