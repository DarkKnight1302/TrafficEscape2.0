using TrafficEscape2._0.Entities;

namespace TrafficEscape2._0.Repositories
{
    public interface IUserRepository
    {
        public Task CreateUser(string userId);

        public Task UpdateUser(User user);

        public Task<User> GetUser(string userId);
    }
}
