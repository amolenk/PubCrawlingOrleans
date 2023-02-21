[GenerateSerializer]
public class CrawlerLocation
{
    [Id(0)] public Guid EventId { get; set; } = Guid.NewGuid();
    [Id(1)] public string VenueId { get; set; } = string.Empty;
}
