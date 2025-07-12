using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace TrafficEscape2._0.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<UserRepository> logger;

        public UserRepository(ICosmosDbService cosmosDbService, ILogger<UserRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task CreateUser(string userId)
        {
            var container = GetContainer();
            Entities.User user = new Entities.User()
            {
                id = userId,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await container.CreateItemAsync<Entities.User>(user, new PartitionKey(userId));
        }

        public async Task<Entities.User> GetUser(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse =  await container.ReadItemAsync<Entities.User>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            } catch (CosmosException)
            {
                return null;
            }
        }

        public async Task UpdateUser(Entities.User user)
        {
            var container = GetContainer();
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await container.UpsertItemAsync(user, new PartitionKey(user.id));
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("User");
        }

    }
}
