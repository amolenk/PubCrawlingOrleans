[GenerateSerializer]
public class VenueSummary
{
    [Id(0)] public string Name { get; set; } = null!;
    [Id(1)] public bool IsCheckedIn { get; set; }
    [Id(2)] public Dictionary<string, int> Beers { get; set; } = new();
}
