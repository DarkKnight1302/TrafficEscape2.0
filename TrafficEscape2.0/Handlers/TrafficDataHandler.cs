using TrafficEscape2._0.Entities;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Handlers
{
    public class TrafficDataHandler : ITrafficDataHandler
    {
        private readonly IUserService userService;
        private readonly ITrafficComputeService trafficComputeService;

        public TrafficDataHandler(IUserService userService, ITrafficComputeService trafficComputeService)
        {
            this.userService = userService;
            this.trafficComputeService = trafficComputeService;
        }

        public async Task<TrafficDataResponse> GetTrafficDataforDay(int dayOfWeek, string userId)
        {
            User user = await this.userService.GetUser(userId);
            if (user == null || user.HomeOffice == null || user.OfficeHome == null)
            {
                throw new Exception("Missing User details");
            }
            TrafficDataResponse trafficDataResponse = new TrafficDataResponse();
            var homeOfficeDataMap = await this.trafficComputeService.GetTimeWithMinTraffic(user.HomePlaceId, user.OfficePlaceId, dayOfWeek, user.HomeOffice.StartTime, user.HomeOffice.EndTime);
            var officeHomeDataMap = await this.trafficComputeService.GetTimeWithMinTraffic(user.OfficePlaceId, user.HomePlaceId, dayOfWeek, user.OfficeHome.StartTime, user.OfficeHome.EndTime);
            trafficDataResponse.HomeToOfficeTrafficData = homeOfficeDataMap;
            trafficDataResponse.OfficeToHomeTrafficData = officeHomeDataMap;
            return trafficDataResponse;
        }
    }
}
