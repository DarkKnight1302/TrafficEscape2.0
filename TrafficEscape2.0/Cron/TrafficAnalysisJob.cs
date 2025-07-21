using NewHorizonLib.Extensions;
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
            DateTimeOffset currentIndiaTime = DateTimeOffset.UtcNow.ToIndiaTime();
            this.logger.LogInformation($"Running traffic analysis job for time {currentIndiaTime}, Previous logged time {DateTimeOffset.Now.ToIndiaTime()}");
            int currentTime = currentIndiaTime.Hour * 100 + currentIndiaTime.Minute;
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
            this.logger.LogInformation($"Time slot picked {timeSlot} - {(int)currentIndiaTime.DayOfWeek}");
            List<RouteSlots> routeSlots = await this.routeSlotRepository.GetAllRoutesForTime((int)currentIndiaTime.DayOfWeek, timeSlot);
            List<RouteSlots> selectedRoutes = FilterRouteSlots(routeSlots);
            if (selectedRoutes.Count == 0)
            {
                this.logger.LogInformation($"Not route slot found for {timeSlot}");
                return;
            }
            foreach (RouteSlots selectedRoute in selectedRoutes)
            {
                int trafficDurationInMins = await this.googleTrafficApiClient.GetRouteDurationInMins(selectedRoute.fromPlaceId, selectedRoute.toPlaceId);
                if (trafficDurationInMins != -1)
                {
                    selectedRoute.durationInMins.Add(trafficDurationInMins);
                    selectedRoute.updatedAt = currentIndiaTime;
                    if (selectedRoute.durationInMins.Count > 50)
                    {
                        selectedRoute.durationInMins.RemoveAt(0);
                    }
                    await this.routeSlotRepository.UpsertSlotData(selectedRoute);
                }
            }
        }

        private static List<RouteSlots> FilterRouteSlots(List<RouteSlots> routeSlots)
        {
            List<RouteSlots> result = new List<RouteSlots>();
            if (routeSlots == null || routeSlots.Count == 0)
            {
                return result;
            }
           
            int minDataCount = Int32.MaxValue;

            foreach(RouteSlots r in routeSlots)
            {
                if (r.durationInMins.Count == 0)
                {
                    result.Add(r);
                    continue;
                }
            }
            if (result.Count == 0)
            {
                RouteSlots minDataSlot = null;
                foreach (RouteSlots r in routeSlots)
                {
                    if (r.durationInMins.Count < minDataCount)
                    {
                        minDataCount = r.durationInMins.Count;
                        minDataSlot = r;
                    }
                }
                if (minDataSlot != null)
                {
                    result.Add(minDataSlot);
                }
            }
            return result;
        }
    }
}
