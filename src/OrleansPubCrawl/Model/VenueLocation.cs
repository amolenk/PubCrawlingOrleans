[GenerateSerializer]
public class VenueLocation
{
    [Id(0)] public string Id { get; set; } = null!;
    [Id(2)] public double Latitude { get; set; }
    [Id(3)] public double Longitude { get; set; }
}
