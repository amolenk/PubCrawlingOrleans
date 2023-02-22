[GenerateSerializer]
public class Venue
{
    [Id(0)] public string Id { get; set; } = null!;
    [Id(1)] public string Name { get; set; } = null!;
    [Id(2)] public double Latitude { get; set; }
    [Id(3)] public double Longitude { get; set; }
    [Id(4)] public List<string> Beers { get; set; } = new();
}
