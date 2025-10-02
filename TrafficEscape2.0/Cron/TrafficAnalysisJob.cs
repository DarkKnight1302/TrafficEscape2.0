using System.Diagnostics;
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
            var jobStopwatch = Stopwatch.StartNew();
            this.logger.LogInformation("=== TrafficAnalysisJob execution started ===");
            this.logger.LogDebug("Job execution context - JobKey: {JobKey}, FireInstanceId: {FireInstanceId}", 
                context.JobDetail.Key, context.FireInstanceId);

            try
            {
                DateTimeOffset currentIndiaTime = DateTimeOffset.UtcNow.ToIndiaTime();
                this.logger.LogInformation("Running traffic analysis job for time {CurrentIndiaTime}, Previous logged time {PreviousTime}", 
                    currentIndiaTime, DateTimeOffset.Now.ToIndiaTime());
                
                // Time slot calculation with detailed logging
                int currentTime = currentIndiaTime.Hour * 100 + currentIndiaTime.Minute;
                this.logger.LogDebug("Calculating time slot - Current time in HHmm format: {CurrentTime} (Hour: {Hour}, Minute: {Minute})", 
                    currentTime, currentIndiaTime.Hour, currentIndiaTime.Minute);
                
                int timeSlot = 0;
                if ((currentTime % 10) < 5)
                {
                    timeSlot = currentTime - currentTime % 10;
                    this.logger.LogDebug("Time slot calculation - Rounding down: {TimeSlot} (mod < 5)", timeSlot);
                } 
                else
                {
                    int mod = currentTime % 10;
                    timeSlot = currentTime + (10 - mod);
                    this.logger.LogDebug("Time slot calculation - Rounding up: {TimeSlot} (mod = {Mod} >= 5)", timeSlot, mod);
                    
                    if (timeSlot % 100 == 60)
                    {
                        int originalTimeSlot = timeSlot;
                        timeSlot = timeSlot - timeSlot % 100;
                        timeSlot += 100;
                        this.logger.LogDebug("Time slot adjusted for hour boundary: {OriginalTimeSlot} -> {NewTimeSlot}", 
                            originalTimeSlot, timeSlot);
                    }
                }
                
                this.logger.LogInformation("Time slot picked {TimeSlot} for day {DayOfWeek}", 
                    timeSlot, (int)currentIndiaTime.DayOfWeek);

                // Route retrieval with timing
                this.logger.LogInformation("Fetching routes from repository for day {DayOfWeek} and timeSlot {TimeSlot}", 
                    (int)currentIndiaTime.DayOfWeek, timeSlot);
                var dbStopwatch = Stopwatch.StartNew();
                List<RouteSlots> routeSlots = await this.routeSlotRepository.GetAllRoutesForTime((int)currentIndiaTime.DayOfWeek, timeSlot);
                dbStopwatch.Stop();
                this.logger.LogInformation("Retrieved {RouteCount} route(s) from repository in {ElapsedMs}ms", 
                    routeSlots?.Count ?? 0, dbStopwatch.ElapsedMilliseconds);

                // Route filtering with detailed logging
                this.logger.LogDebug("Starting route filtering process with {InputCount} routes", routeSlots?.Count ?? 0);
                List<RouteSlots> selectedRoutes = FilterRouteSlots(routeSlots);
                this.logger.LogInformation("Route filtering completed - Selected {SelectedCount} out of {TotalCount} routes", 
                    selectedRoutes.Count, routeSlots?.Count ?? 0);
                
                if (selectedRoutes.Count == 0)
                {
                    this.logger.LogWarning("No route slot found for timeSlot {TimeSlot} on day {DayOfWeek}. Job completing without processing.", 
                        timeSlot, (int)currentIndiaTime.DayOfWeek);
                    return;
                }

                // Process each selected route
                int successCount = 0;
                int failureCount = 0;
                this.logger.LogInformation("Processing {RouteCount} selected routes", selectedRoutes.Count);

                foreach (RouteSlots selectedRoute in selectedRoutes)
                {
                    this.logger.LogDebug("Processing route - ID: {RouteId}, From: {FromPlaceId}, To: {ToPlaceId}, Current data points: {DataPointCount}", 
                        selectedRoute.id, selectedRoute.fromPlaceId, selectedRoute.toPlaceId, selectedRoute.durationInMins.Count);

                    // API call with timing
                    var apiStopwatch = Stopwatch.StartNew();
                    int trafficDurationInMins = await this.googleTrafficApiClient.GetRouteDurationInMins(selectedRoute.fromPlaceId, selectedRoute.toPlaceId);
                    apiStopwatch.Stop();

                    if (trafficDurationInMins != -1)
                    {
                        this.logger.LogInformation("Google Traffic API success - Route: {RouteId}, Duration: {Duration}mins, API response time: {ResponseTimeMs}ms", 
                            selectedRoute.id, trafficDurationInMins, apiStopwatch.ElapsedMilliseconds);

                        int previousDataPointCount = selectedRoute.durationInMins.Count;
                        selectedRoute.durationInMins.Add(trafficDurationInMins);
                        selectedRoute.updatedAt = currentIndiaTime;
                        
                        if (selectedRoute.durationInMins.Count > 50)
                        {
                            int removedDuration = selectedRoute.durationInMins[0];
                            selectedRoute.durationInMins.RemoveAt(0);
                            this.logger.LogDebug("Route data limit reached - Removed oldest data point: {RemovedDuration}mins for route {RouteId}", 
                                removedDuration, selectedRoute.id);
                        }

                        this.logger.LogDebug("Route data updated - Route: {RouteId}, Data points: {PreviousCount} -> {NewCount}", 
                            selectedRoute.id, previousDataPointCount, selectedRoute.durationInMins.Count);

                        // Database upsert with timing
                        var upsertStopwatch = Stopwatch.StartNew();
                        try
                        {
                            await this.routeSlotRepository.UpsertSlotData(selectedRoute);
                            upsertStopwatch.Stop();
                            this.logger.LogInformation("Database upsert successful - Route: {RouteId}, Duration: {UpsertTimeMs}ms", 
                                selectedRoute.id, upsertStopwatch.ElapsedMilliseconds);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            upsertStopwatch.Stop();
                            this.logger.LogError(ex, "Database upsert failed - Route: {RouteId}, FromPlaceId: {FromPlaceId}, ToPlaceId: {ToPlaceId}, Error: {ErrorMessage}", 
                                selectedRoute.id, selectedRoute.fromPlaceId, selectedRoute.toPlaceId, ex.Message);
                            failureCount++;
                        }
                    }
                    else
                    {
                        this.logger.LogWarning("Google Traffic API returned invalid duration (-1) - Route: {RouteId}, From: {FromPlaceId}, To: {ToPlaceId}, API call time: {ResponseTimeMs}ms", 
                            selectedRoute.id, selectedRoute.fromPlaceId, selectedRoute.toPlaceId, apiStopwatch.ElapsedMilliseconds);
                        failureCount++;
                    }
                }

                // Job summary
                jobStopwatch.Stop();
                this.logger.LogInformation("=== TrafficAnalysisJob execution completed === Total time: {TotalTimeMs}ms, Success: {SuccessCount}, Failures: {FailureCount}", 
                    jobStopwatch.ElapsedMilliseconds, successCount, failureCount);
            }
            catch (Exception ex)
            {
                jobStopwatch.Stop();
                this.logger.LogError(ex, "=== TrafficAnalysisJob execution FAILED === Total time: {TotalTimeMs}ms, Error: {ErrorMessage}, StackTrace: {StackTrace}", 
                    jobStopwatch.ElapsedMilliseconds, ex.Message, ex.StackTrace);
                throw;
            }
        }

        private List<RouteSlots> FilterRouteSlots(List<RouteSlots> routeSlots)
        {
            this.logger.LogDebug("FilterRouteSlots method entry - Input routes count: {RouteCount}", routeSlots?.Count ?? 0);
            
            List<RouteSlots> result = new List<RouteSlots>();
            if (routeSlots == null || routeSlots.Count == 0)
            {
                this.logger.LogDebug("FilterRouteSlots - No routes to filter (null or empty list)");
                return result;
            }
           
            int minDataCount = Int32.MaxValue;
            int emptyDataRoutes = 0;

            // First pass: find routes with no data
            this.logger.LogDebug("FilterRouteSlots - First pass: looking for routes with no existing data");
            foreach(RouteSlots r in routeSlots)
            {
                if (r.durationInMins.Count == 0)
                {
                    result.Add(r);
                    emptyDataRoutes++;
                    this.logger.LogDebug("FilterRouteSlots - Found route with no data: RouteId={RouteId}, From={FromPlaceId}, To={ToPlaceId}", 
                        r.id, r.fromPlaceId, r.toPlaceId);
                    continue;
                }
            }
            
            if (result.Count == 0)
            {
                this.logger.LogDebug("FilterRouteSlots - No routes with empty data found. Second pass: finding route with minimum data points");
                
                RouteSlots minDataSlot = null;
                foreach (RouteSlots r in routeSlots)
                {
                    this.logger.LogDebug("FilterRouteSlots - Evaluating route: RouteId={RouteId}, DataPointCount={DataPointCount}", 
                        r.id, r.durationInMins.Count);
                    
                    if (r.durationInMins.Count < minDataCount)
                    {
                        minDataCount = r.durationInMins.Count;
                        minDataSlot = r;
                        this.logger.LogDebug("FilterRouteSlots - New minimum found: RouteId={RouteId}, MinDataCount={MinDataCount}", 
                            r.id, minDataCount);
                    }
                }
                
                if (minDataSlot != null)
                {
                    result.Add(minDataSlot);
                    this.logger.LogInformation("FilterRouteSlots - Selected route with minimum data: RouteId={RouteId}, DataPointCount={DataPointCount}", 
                        minDataSlot.id, minDataCount);
                }
                else
                {
                    this.logger.LogWarning("FilterRouteSlots - No route selected (minDataSlot is null)");
                }
            }
            else
            {
                this.logger.LogInformation("FilterRouteSlots - Selected {EmptyDataRoutes} route(s) with no existing data", emptyDataRoutes);
            }
            
            this.logger.LogDebug("FilterRouteSlots method exit - Returning {ResultCount} route(s)", result.Count);
            return result;
        }
    }
}
