using TrafficEscape2._0.Entities;

namespace TrafficEscape2._0.Repositories
{
    public interface IRouteSlotRepository
    {
        public Task<List<RouteSlots>> GetSlotData(string fromPlaceId, string toPlaceId, int dayOfWeek);

        public Task InsertSlotIfNotExist(string fromPlaceId, string toPlaceId, int dayOfWeek, int timeSlot);

        public Task UpsertSlotData(RouteSlots routeSlots);
    }
}
