namespace BlazorPubCrawl.Model;

public record Venue(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int Attendance);