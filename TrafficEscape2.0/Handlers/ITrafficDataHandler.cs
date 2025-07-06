using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Handlers
{
    public interface ITrafficDataHandler
    {
        public Task<TrafficDataResponse> GetTrafficDataforDay(int dayOfWeek, string userId);
    }
}
