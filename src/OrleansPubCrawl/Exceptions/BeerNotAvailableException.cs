public class BeerNotAvailableException : Exception
{
    public BeerNotAvailableException(string beerId, long eventId)
        : base($"The beer {beerId} is not available in event {eventId}.")
    {
    }
}