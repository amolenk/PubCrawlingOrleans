// Signalr integration is based on the Orleans GPS sample:
// https://learn.microsoft.com/en-us/samples/dotnet/samples/orleans-gps-device-tracker-sample/

using Orleans.Runtime;

public interface IEventMapHubListGrain : IGrainWithGuidKey
{
    ValueTask AddHubAsync(SiloAddress host, IEventMapHubProxy hub);
    
    ValueTask<List<IEventMapHubProxy>> GetHubsAsync();
}

public class EventMapHubListGrain : Grain, IEventMapHubListGrain
{
    private readonly IClusterMembershipService _clusterMembership;
    private readonly Dictionary<SiloAddress, IEventMapHubProxy> _hubs = new();
    private MembershipVersion _cacheMembershipVersion;
    private List<IEventMapHubProxy>? _cache;

    public EventMapHubListGrain(IClusterMembershipService clusterMembershipService)
    {
        _clusterMembership = clusterMembershipService;
    }

    public ValueTask AddHubAsync(SiloAddress host, IEventMapHubProxy hub)
    {
        // Invalidate the cache.
        _cache = null;
        _hubs[host] = hub;

        return default;
    }

    public ValueTask<List<IEventMapHubProxy>> GetHubsAsync() =>
        new(GetCachedHubs());

    private List<IEventMapHubProxy> GetCachedHubs()
    {
        // Returns a cached list of hubs if the cache is valid, otherwise builds a list of hubs.
        ClusterMembershipSnapshot clusterMembers = _clusterMembership.CurrentSnapshot;
        if (_cache is { } && clusterMembers.Version == _cacheMembershipVersion)
        {
            return _cache;
        }

        // Filter out hosts which are not yet active or have been removed from the cluster.
        var hubs = new List<IEventMapHubProxy>();
        var toDelete = new List<SiloAddress>();

        foreach (KeyValuePair<SiloAddress, IEventMapHubProxy> pair in _hubs)
        {
            SiloAddress host = pair.Key;
            IEventMapHubProxy hub = pair.Value;
            SiloStatus hostStatus = clusterMembers.GetSiloStatus(host);
            if (hostStatus is SiloStatus.Dead)
            {
                toDelete.Add(host);
            }

            if (hostStatus is SiloStatus.Active)
            {
                hubs.Add(hub);
            }
        }

        foreach (SiloAddress host in toDelete)
        {
            _hubs.Remove(host);
        }

        _cache = hubs;
        _cacheMembershipVersion = clusterMembers.Version;
        return hubs;
    }
}