namespace BlazorPubCrawl.Model;

public record CrawlerStatus(
    string VenueId,
    Dictionary<string, int> BeerRatings);