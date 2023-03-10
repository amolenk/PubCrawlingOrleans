[GenerateSerializer]
public class CrawlerNotCheckedInException : Exception
{
    public CrawlerNotCheckedInException(string crawlerId, string venueId, long eventId)
        : base($"The crawler {crawlerId} is not checked in at venue {venueId} in event {eventId}.")
    {
    }
}