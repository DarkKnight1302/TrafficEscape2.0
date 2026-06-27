using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Services.Interfaces
{
    public interface ITrafficComputeService
    {
        public Task InsertAllSlotsForTimeRange(string fromPlaceId, string toPlaceId, int startTime, int endTime);

        public Task<Dictionary<int, TrafficTimeData>> GetTimeWithMinTraffic(string fromPlaceId, string toPlaceId, int dayOfWeek, int startTime, int endTime);
    }
}
