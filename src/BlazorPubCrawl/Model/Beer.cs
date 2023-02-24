namespace BlazorPubCrawl.Model;

public record Beer(
    string Id,
    string Name,
    string Brewery,
    string Style,
    string Description,
    decimal Abv);
