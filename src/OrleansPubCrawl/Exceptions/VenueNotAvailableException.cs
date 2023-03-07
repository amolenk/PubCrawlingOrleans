public class VenueNotAvailableException : Exception
{
    public VenueNotAvailableException(string venueId, long eventId)
        : base($"Venue {venueId} does not take part in event {eventId}.")
    {
    }
}
