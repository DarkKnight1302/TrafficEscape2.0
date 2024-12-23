using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Services.Interfaces
{
    public interface IUserService
    {
        public Task RegisterUser(string userId);

        public Task UpdateUser(UserUpdateRequest userUpdateRequest);

        public Task<bool> IsAnalysisCompleted(string userId);

        public Task<BestTime> GetBestTrafficTime(string userId);
    }
}
