[GenerateSerializer]
public class Beer
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string Type { get; set; } = string.Empty;
    [Id(3)] public string Description { get; set; } = string.Empty;
}
