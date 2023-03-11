namespace BlazorPubCrawl.Model;

public record BeerHighscore(
    string Id,
    string Name,
    string Brewery,
    string Style,
    decimal Abv,
    int Score);
