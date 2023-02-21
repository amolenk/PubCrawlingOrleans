using Newtonsoft.Json;

public class GrainDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("grainType")]
    public string GrainType { get; set; } = null!;

    [JsonProperty("grainId")]
    public string GrainId { get; set; } = null!;

    [JsonProperty("state")]
    public string State { get; set; } = null!;
}
