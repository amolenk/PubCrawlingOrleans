public interface IHandleVenueEvents : IGrainObserver
{
    Task OnNumberOfCrawlersChangedAsync(string venueId, int crawlerCount);
}