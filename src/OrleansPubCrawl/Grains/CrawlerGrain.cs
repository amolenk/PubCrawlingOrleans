
public interface ICrawlerGrain : IGrainWithStringKey
{
}

public class CrawlerGrain : Grain, ICrawlerGrain
{
}