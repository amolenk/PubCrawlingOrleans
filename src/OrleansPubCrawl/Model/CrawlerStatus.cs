[GenerateSerializer]
public class CrawlerStatus
{
    [Id(0)] public string VenueId { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, int> BeerRatings { get; set; } = new();
}
