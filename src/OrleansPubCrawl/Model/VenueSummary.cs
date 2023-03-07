[GenerateSerializer]
public class VenueSummary
{
    [Id(0)] public string Name { get; set; } = null!;
    [Id(1)] public List<string> Beers { get; set; } = new();
}
