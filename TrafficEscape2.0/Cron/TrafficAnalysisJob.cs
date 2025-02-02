using Quartz;
using TrafficEscape2._0.ApiClients;
using TrafficEscape2._0.Entities;
using TrafficEscape2._0.Repositories;

namespace TrafficEscape2._0.Cron
{
    public class TrafficAnalysisJob : IJob
    {
        private readonly ILogger<TrafficAnalysisJob> logger;
        private readonly IRouteSlotRepository routeSlotRepository;
        private readonly IGoogleTrafficApiClient googleTrafficApiClient;

        public TrafficAnalysisJob(ILogger<TrafficAnalysisJob> logger,
            IRouteSlotRepository routeSlotRepository,
            IGoogleTrafficApiClient googleTrafficApiClient)
        {
            this.logger = logger;
            this.routeSlotRepository = routeSlotRepository;
            this.googleTrafficApiClient = googleTrafficApiClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            int currentTime = DateTime.UtcNow.Hour * 100 + DateTime.UtcNow.Minute;
            int timeSlot = 0;
            if ((currentTime % 10) < 5)
            {
                timeSlot = currentTime - currentTime % 10;
            } else
            {
                int mod = currentTime % 10;
                timeSlot = currentTime + (10 - mod);
                if (timeSlot % 100 == 60)
                {
                    timeSlot = timeSlot - timeSlot % 100;
                    timeSlot += 100;
                }
            }

            List<RouteSlots> routeSlots = await this.routeSlotRepository.GetAllRoutesForTime((int)DateTime.UtcNow.DayOfWeek, timeSlot);
            RouteSlots selectedRoute = FilterRouteSlots(routeSlots);
            if (selectedRoute == null)
            {
                this.logger.LogInformation($"Not route slot found for {timeSlot}");
                return;
            }
            int trafficDurationInMins = await this.googleTrafficApiClient.GetRouteDurationInMins(selectedRoute.fromPlaceId, selectedRoute.toPlaceId);
            if (trafficDurationInMins != -1)
            {
                selectedRoute.durationInMins.Add(trafficDurationInMins);
                selectedRoute.updatedAt = DateTimeOffset.UtcNow;
                await this.routeSlotRepository.UpsertSlotData(selectedRoute);
            }
        }

        private static RouteSlots FilterRouteSlots(List<RouteSlots> routeSlots)
        {
            if (routeSlots == null || routeSlots.Count == 0)
            {
                return null;
            }
            RouteSlots result = null;
            int minDataCount = Int32.MaxValue;
            foreach(RouteSlots r in routeSlots)
            {
                if (r.durationInMins.Count < minDataCount)
                {
                    minDataCount = r.durationInMins.Count;
                    result = r;
                }
            }
            return result;
        }
    }
}
