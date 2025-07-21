namespace TrafficEscape2._0.ApiClients
{
    public interface IGoogleTrafficApiClient
    {
        public Task<int> GetRouteDurationInMins(string fromPlaceId, string toPlaceId, bool ignoreThreshold = false);
    }
}
