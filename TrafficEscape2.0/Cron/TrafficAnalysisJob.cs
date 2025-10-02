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
            this.logger.LogDebug("TrafficAnalysisJob initialized successfully");
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            this.logger.LogInformation("Starting traffic analysis job execution");
            
            try
            {
                DateTimeOffset currentIndiaTime = DateTimeOffset.UtcNow.ToIndiaTime();
                this.logger.LogInformation($"Running traffic analysis job for time {currentIndiaTime}, Previous logged time {DateTimeOffset.Now.ToIndiaTime()}");
                
                int currentTime = currentIndiaTime.Hour * 100 + currentIndiaTime.Minute;
                this.logger.LogDebug($"Current time calculated: {currentTime} (Hour: {currentIndiaTime.Hour}, Minute: {currentIndiaTime.Minute})");
                
                int timeSlot = CalculateTimeSlot(currentTime);
                this.logger.LogInformation($"Time slot calculated: {timeSlot} for day of week: {(int)currentIndiaTime.DayOfWeek}");

                List<RouteSlots> routeSlots = await GetRouteSlots(currentIndiaTime, timeSlot);
                if (routeSlots == null || routeSlots.Count == 0)
                {
                    this.logger.LogWarning($"No route slots found for time slot {timeSlot} and day {(int)currentIndiaTime.DayOfWeek}");
                    return;
                }

                this.logger.LogInformation($"Retrieved {routeSlots.Count} route slots from repository");

                List<RouteSlots> selectedRoutes = FilterRouteSlots(routeSlots);
                if (selectedRoutes.Count == 0)
                {
                    this.logger.LogInformation($"No route slot selected after filtering for time slot {timeSlot}");
                    return;
                }

                this.logger.LogInformation($"Selected {selectedRoutes.Count} routes for processing after filtering");

                await ProcessSelectedRoutes(selectedRoutes, currentIndiaTime);
                
                stopwatch.Stop();
                this.logger.LogInformation($"Traffic analysis job completed successfully in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                this.logger.LogError(ex, $"Traffic analysis job failed after {stopwatch.ElapsedMilliseconds}ms. Error: {ex.Message}");
                throw;
            }
        }

        private int CalculateTimeSlot(int currentTime)
        {
            this.logger.LogDebug($"Calculating time slot for current time: {currentTime}");
            
            int timeSlot = 0;
            if ((currentTime % 10) < 5)
            {
                timeSlot = currentTime - currentTime % 10;
                this.logger.LogDebug($"Time slot calculated using first condition: {timeSlot}");
            } 
            else
            {
                int mod = currentTime % 10;
                timeSlot = currentTime + (10 - mod);
                this.logger.LogDebug($"Initial time slot calculation: {timeSlot}, mod: {mod}");
                
                if (timeSlot % 100 == 60)
                {
                    int originalTimeSlot = timeSlot;
                    timeSlot = timeSlot - timeSlot % 100;
                    timeSlot += 100;
                    this.logger.LogDebug($"Adjusted time slot from {originalTimeSlot} to {timeSlot} due to 60-minute overflow");
                }
            }
            
            this.logger.LogDebug($"Final calculated time slot: {timeSlot}");
            return timeSlot;
        }

        private async Task<List<RouteSlots>> GetRouteSlots(DateTimeOffset currentIndiaTime, int timeSlot)
        {
            this.logger.LogDebug($"Fetching route slots for day {(int)currentIndiaTime.DayOfWeek} and time slot {timeSlot}");
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                List<RouteSlots> routeSlots = await this.routeSlotRepository.GetAllRoutesForTime((int)currentIndiaTime.DayOfWeek, timeSlot);
                stopwatch.Stop();
                
                this.logger.LogDebug($"Repository query completed in {stopwatch.ElapsedMilliseconds}ms, returned {routeSlots?.Count ?? 0} route slots");
                return routeSlots;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Failed to retrieve route slots from repository for day {(int)currentIndiaTime.DayOfWeek} and time slot {timeSlot}");
                throw;
            }
        }

        private async Task ProcessSelectedRoutes(List<RouteSlots> selectedRoutes, DateTimeOffset currentIndiaTime)
        {
            this.logger.LogInformation($"Processing {selectedRoutes.Count} selected routes");
            int successCount = 0;
            int failureCount = 0;
            
            foreach (RouteSlots selectedRoute in selectedRoutes)
            {
                try
                {
                    this.logger.LogDebug($"Processing route from {selectedRoute.fromPlaceId} to {selectedRoute.toPlaceId}");
                    
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    int trafficDurationInMins = await this.googleTrafficApiClient.GetRouteDurationInMins(selectedRoute.fromPlaceId, selectedRoute.toPlaceId);
                    stopwatch.Stop();
                    
                    if (trafficDurationInMins != -1)
                    {
                        this.logger.LogDebug($"Google API returned duration: {trafficDurationInMins} minutes in {stopwatch.ElapsedMilliseconds}ms for route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId}");
                        
                        int originalCount = selectedRoute.durationInMins.Count;
                        selectedRoute.durationInMins.Add(trafficDurationInMins);
                        selectedRoute.updatedAt = currentIndiaTime;
                        
                        if (selectedRoute.durationInMins.Count > 50)
                        {
                            selectedRoute.durationInMins.RemoveAt(0);
                            this.logger.LogDebug($"Removed oldest duration entry, maintaining 50-item limit for route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId}");
                        }
                        
                        this.logger.LogDebug($"Duration list updated from {originalCount} to {selectedRoute.durationInMins.Count} entries");
                        
                        try
                        {
                            await this.routeSlotRepository.UpsertSlotData(selectedRoute);
                            this.logger.LogDebug($"Successfully upserted data for route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId}");
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"Failed to upsert slot data for route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId}");
                            failureCount++;
                        }
                    }
                    else
                    {
                        this.logger.LogWarning($"Google API returned invalid duration (-1) for route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId} after {stopwatch.ElapsedMilliseconds}ms");
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Error processing route {selectedRoute.fromPlaceId} -> {selectedRoute.toPlaceId}: {ex.Message}");
                    failureCount++;
                }
            }
            
            this.logger.LogInformation($"Route processing completed. Success: {successCount}, Failures: {failureCount}");
        }

        private List<RouteSlots> FilterRouteSlots(List<RouteSlots> routeSlots)
        {
            this.logger.LogDebug($"Filtering {routeSlots?.Count ?? 0} route slots");
            
            List<RouteSlots> result = new List<RouteSlots>();
            if (routeSlots == null || routeSlots.Count == 0)
            {
                this.logger.LogDebug("No route slots to filter, returning empty result");
                return result;
            }
           
            int minDataCount = Int32.MaxValue;
            int routesWithNoData = 0;

            // First pass: find routes with no data
            foreach(RouteSlots r in routeSlots)
            {
                if (r.durationInMins.Count == 0)
                {
                    result.Add(r);
                    routesWithNoData++;
                    this.logger.LogDebug($"Added route {r.fromPlaceId} -> {r.toPlaceId} with no existing data");
                }
            }
            
            this.logger.LogDebug($"Found {routesWithNoData} routes with no existing data");
            
            // Second pass: if no routes without data, find route with minimum data
            if (result.Count == 0)
            {
                this.logger.LogDebug("No routes without data found, selecting route with minimum data count");
                
                RouteSlots minDataSlot = null;
                foreach (RouteSlots r in routeSlots)
                {
                    if (r.durationInMins.Count < minDataCount)
                    {
                        minDataCount = r.durationInMins.Count;
                        minDataSlot = r;
                        this.logger.LogDebug($"New minimum data count: {minDataCount} for route {r.fromPlaceId} -> {r.toPlaceId}");
                    }
                }
                
                if (minDataSlot != null)
                {
                    result.Add(minDataSlot);
                    this.logger.LogDebug($"Selected route {minDataSlot.fromPlaceId} -> {minDataSlot.toPlaceId} with minimum data count: {minDataCount}");
                }
                else
                {
                    this.logger.LogWarning("Could not find any route to select during filtering");
                }
            }
            
            this.logger.LogInformation($"Route filtering completed. Selected {result.Count} routes out of {routeSlots.Count} total routes");
            return result;
        }
    }
}
