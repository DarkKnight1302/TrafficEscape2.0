using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;
using TrafficEscape2._0.Entities;

namespace TrafficEscape2._0.Repositories
{
    public class RouteSlotRepository : IRouteSlotRepository
    {
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<RouteSlotRepository> logger;

        public RouteSlotRepository(ICosmosDbService cosmosDbService, ILogger<RouteSlotRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task<List<RouteSlots>> GetSlotData(string fromPlaceId, string toPlaceId, int dayOfWeek)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.fromPlaceId = @fromPlaceId AND c.toPlaceId = @toPlaceId AND c.dayOfWeek = @dayOfWeek"
            )
            .WithParameter("@fromPlaceId", fromPlaceId)
            .WithParameter("@toPlaceId", toPlaceId)
            .WithParameter("@dayOfWeek", dayOfWeek);

            var results = new List<RouteSlots>();
            using (var iterator = container.GetItemQueryIterator<RouteSlots>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(dayOfWeek) }))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }
            }

            return results;
        }

        public async Task InsertSlotIfNotExist(string fromPlaceId, string toPlaceId, int dayOfWeek, int timeSlot)
        {
            RouteSlots routeSlots = new RouteSlots()
            {
                id = GenerateUid(fromPlaceId, toPlaceId, dayOfWeek, timeSlot),
                fromPlaceId = fromPlaceId,
                toPlaceId = toPlaceId,
                dayOfWeek = dayOfWeek,
                uid = GenerateUid(fromPlaceId, toPlaceId, dayOfWeek, timeSlot),
                timeSlot = timeSlot,
                updatedAt = DateTimeOffset.UtcNow
            };
            var container = GetContainer();
            await container.UpsertItemAsync(routeSlots).ConfigureAwait(false);
        }

        private string GenerateUid(string fromPlaceId, string toPlaceId, int dayOfWeek, int timeSlot)
        {
            return $"{fromPlaceId}-{toPlaceId}-{dayOfWeek}-{timeSlot}";
        }

        public async Task UpsertSlotData(RouteSlots routeSlots)
        {
            var container = GetContainer();
            await container.UpsertItemAsync(routeSlots).ConfigureAwait(false);
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("RouteSlots");
        }

        public async Task<List<RouteSlots>> GetAllRoutesForTime(int dayOfWeek, int timeSlot)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.dayOfWeek = @dayOfWeek AND c.timeSlot = @timeSlot"
            )
            .WithParameter("@dayOfWeek", dayOfWeek)
            .WithParameter("@timeSlot", timeSlot);

            var results = new List<RouteSlots>();
            using (var iterator = container.GetItemQueryIterator<RouteSlots>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(dayOfWeek) }))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }
            }

            return results;
        }
    }
}
