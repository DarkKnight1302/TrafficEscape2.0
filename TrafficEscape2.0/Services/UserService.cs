﻿using TrafficEscape2._0.Constants;
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

        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;    
        }

        public async Task<BestTime> GetBestTrafficTime(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist", ErrorCodes.UserNotFound);
            }
            BestTime bestTime = new BestTime()
            {
                BestHomeToOfficeTime = user.BestTimeHomeOffice,
                BestOfficeToHomeTime = user.BestTimeOfficeHome
            };
            return bestTime;
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

        public async Task RegisterUser(string userId)
        {
            User user = await this.userRepository.GetUser(userId);
            if (user != null)
            {
                // already exists;
                return;
            }
            await this.userRepository.CreateUser(userId);
        }

        public async Task UpdateUser(UserUpdateRequest userUpdateRequest)
        {
            User user = await this.userRepository.GetUser(userUpdateRequest.UserId);
            if (user == null)
            {
                throw new TrafficEscapeException("User doesn't exist for update", ErrorCodes.UserNotFound);
            }
            user = UpdateUserObj(userUpdateRequest, user);
            await this.userRepository.UpdateUser(user).ConfigureAwait(false);
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
