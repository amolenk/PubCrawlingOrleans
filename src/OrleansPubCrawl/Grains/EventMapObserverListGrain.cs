// Signalr integration is based on the Orleans GPS sample:
// https://learn.microsoft.com/en-us/samples/dotnet/samples/orleans-gps-device-tracker-sample/

using Orleans.Runtime;

public interface IEventMapObserverListGrain : IGrainWithGuidKey
{
    ValueTask AddObserverAsync(SiloAddress host, IEventMapObserver observer);
    
    ValueTask<List<IEventMapObserver>> GetObserversAsync();
}

public class EventMapObserverListGrain : Grain, IEventMapObserverListGrain
{
    private readonly IClusterMembershipService _clusterMembership;
    private readonly Dictionary<SiloAddress, IEventMapObserver> _observers = new();
    private MembershipVersion _cacheMembershipVersion;
    private List<IEventMapObserver>? _cache;

    public EventMapObserverListGrain(IClusterMembershipService clusterMembershipService)
    {
        _clusterMembership = clusterMembershipService;
    }

    public ValueTask AddObserverAsync(SiloAddress host, IEventMapObserver observer)
    {
        // Invalidate the cache.
        _cache = null;
        _observers[host] = observer;

        return default;
    }

    public ValueTask<List<IEventMapObserver>> GetObserversAsync() =>
        new(GetCachedObservers());

    private List<IEventMapObserver> GetCachedObservers()
    {
        // Returns a cached list of hubs if the cache is valid, otherwise builds a list of hubs.
        ClusterMembershipSnapshot clusterMembers = _clusterMembership.CurrentSnapshot;
        if (_cache is { } && clusterMembers.Version == _cacheMembershipVersion)
        {
            return _cache;
        }

        // Filter out hosts which are not yet active or have been removed from the cluster.
        var observers = new List<IEventMapObserver>();
        var toDelete = new List<SiloAddress>();

        foreach (KeyValuePair<SiloAddress, IEventMapObserver> pair in _observers)
        {
            SiloAddress host = pair.Key;
            IEventMapObserver observer = pair.Value;
            SiloStatus hostStatus = clusterMembers.GetSiloStatus(host);
            if (hostStatus is SiloStatus.Dead)
            {
                toDelete.Add(host);
            }

            if (hostStatus is SiloStatus.Active)
            {
                observers.Add(observer);
            }
        }

        foreach (SiloAddress host in toDelete)
        {
            _observers.Remove(host);
        }

        _cache = observers;
        _cacheMembershipVersion = clusterMembers.Version;
        return observers;
    }
}