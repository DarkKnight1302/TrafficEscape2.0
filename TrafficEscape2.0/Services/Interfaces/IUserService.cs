using TrafficEscape2._0.Entities;
using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Services.Interfaces
{
    public interface IUserService
    {
        public Task RegisterUser(string userId);

        public Task UpdateUser(UserUpdateRequest userUpdateRequest);

        public Task<int> GetCompletionDays(string userId);

        public Task<User> GetUser(string userId);

    }
}
