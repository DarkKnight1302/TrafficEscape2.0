using TrafficEscape2._0.ApiClients;
using TrafficEscape2._0.Constants;
using TrafficEscape2._0.Entities;
using TrafficEscape2._0.Exceptions;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Repositories;
using TrafficEscape2._0.Services.Interfaces;
using User = TrafficEscape2._0.Entities.User;

namespace TrafficEscape2._0.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly ITrafficComputeService trafficComputeService;
        private readonly IGoogleTrafficApiClient googleTrafficApiClient;

        public UserService(IUserRepository userRepository, ITrafficComputeService trafficComputeService, IGoogleTrafficApiClient googleTrafficApiClient)
        {
            this.userRepository = userRepository;
            this.trafficComputeService = trafficComputeService;
            this.googleTrafficApiClient = googleTrafficApiClient;
        }

        public async Task<int> GetCompletionDays(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist", ErrorCodes.UserNotFound);
            }
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            return (currentTime - user.AnalysisStartTime).Days >= GlobalConstants.AnalysisDays ? 0 : (GlobalConstants.AnalysisDays - ((currentTime - user.AnalysisStartTime).Days));
        }

        public async Task<User> GetUser(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist", ErrorCodes.UserNotFound);
            }
            return user;
        }

        public async Task<bool> IsAnalysisCompleted(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist", ErrorCodes.UserNotFound);
            }
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            return (currentTime - user.AnalysisStartTime).Days > GlobalConstants.AnalysisDays;
        }

        public async Task<User> RegisterUser(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user != null)
            {
                // already exists;
                return user;
            }
            return await this.userRepository.CreateUser(userId);
        }

        public async Task UpdateUser(UserUpdateRequest userUpdateRequest)
        {
            User user = await this.userRepository.GetUser(userUpdateRequest.UserId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist for update", ErrorCodes.UserNotFound);
            }
            if (IsSameSetting(userUpdateRequest, user))
            {
                // do nothing
                return;
            }
            bool areValidLoc = await AreValidLocations(userUpdateRequest.HomePlaceId, userUpdateRequest.OfficePlaceId);

            if (!areValidLoc)
            {
                throw new TrafficEscapeException("Invalid location Ids", 123);
            }
            user = UpdateUserObj(userUpdateRequest, user);
            await this.userRepository.UpdateUser(user).ConfigureAwait(false);
            await this.trafficComputeService.InsertAllSlotsForTimeRange(user.HomePlaceId, user.OfficePlaceId, user.HomeOffice.StartTime, user.HomeOffice.EndTime);
            await this.trafficComputeService.InsertAllSlotsForTimeRange(user.OfficePlaceId, user.HomePlaceId, user.OfficeHome.StartTime, user.OfficeHome.EndTime);
        }

        private async Task<bool> AreValidLocations(string homePlaceId, string officePlaceId)
        {
            int retryCount = 3;
            do
            {
                int result = await this.googleTrafficApiClient.GetRouteDurationInMins(homePlaceId, officePlaceId, true);
                if (result != -1)
                {
                    return true;
                }
                await Task.Delay(1000);
                retryCount--;
            } while (retryCount > 0);

            return false;
        }

        private bool IsSameSetting(UserUpdateRequest userUpdateRequest, User user)
        {
            if (string.IsNullOrEmpty(user.HomePlaceId))
            {
                return false;
            }
            if (!user.HomePlaceId.Equals(userUpdateRequest.HomePlaceId))
            {
                return false;
            }
            if (!user.OfficePlaceId.Equals(userUpdateRequest.OfficePlaceId))
            {
                return false;
            }
            if (user.HomeOffice.StartTime != userUpdateRequest.HomeToOfficeStartTime)
            {
                return false;
            }
            if (user.HomeOffice.EndTime != userUpdateRequest.HomeToOfficeEndTime)
            {
                return false;
            }
            if (user.OfficeHome.StartTime != userUpdateRequest.OfficeToHomeStartTime)
            {
                return false;
            }
            if (user.OfficeHome.EndTime != userUpdateRequest.OfficeToHomeEndTime)
            {
                return false;
            }
            return true;
        }

        private User UpdateUserObj(UserUpdateRequest userUpdate, User user)
        {
            if (!string.IsNullOrEmpty(userUpdate.HomePlaceId))
            {
                user.HomePlaceId = userUpdate.HomePlaceId;
            }
            if (!string.IsNullOrEmpty(userUpdate.OfficePlaceId))
            {
                user.OfficePlaceId = userUpdate.OfficePlaceId;
            }
            TimeRange homeOffice = new TimeRange()
            {
                StartTime = userUpdate.HomeToOfficeStartTime,
                EndTime = userUpdate.HomeToOfficeEndTime
            };
            user.HomeOffice = homeOffice;
            TimeRange officeHome = new TimeRange()
            {
                StartTime = userUpdate.OfficeToHomeStartTime,
                EndTime = userUpdate.OfficeToHomeEndTime
            };
            user.OfficeHome = officeHome;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.AnalysisStartTime = DateTimeOffset.UtcNow;
            return user;
        }
    }
}
