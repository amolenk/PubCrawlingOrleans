public interface IHandleVenueEvents : IGrainObserver
{
    Task OnNumberOfCrawlersChangedAsync(int crawlerCount);
}