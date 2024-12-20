using TrafficEscape2._0.Entities;
using TrafficEscape2._0.Repositories;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Services
{
    public class TrafficComputeService : ITrafficComputeService
    {
        private readonly IRouteSlotRepository routeSlotRepository;
        private readonly ILogger<TrafficComputeService> logger;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public TrafficComputeService(IRouteSlotRepository routeSlotRepository, ILogger<TrafficComputeService> logger)
        {
            this.routeSlotRepository = routeSlotRepository;
            this.logger = logger;
        }

        public async Task<int> GetTimeWithMinTraffic(string fromPlaceId, string toPlaceId, int dayOfWeek, int startTime, int endTime)
        {
            var timeSlots = GetAllTimeSlots(startTime, endTime);
            var routeSlots = await this.routeSlotRepository.GetSlotData(fromPlaceId, toPlaceId, dayOfWeek);
            var routeSlotDictionary = this.ConvertToRouteSlotDictionary(routeSlots);
            int minTrafficTime = Int32.MaxValue;
            int bestTimeSlot = -1;
            foreach(int time in timeSlots)
            {
                var routeSlot = routeSlotDictionary[time];
                int trafficTime = FindMedianTime(routeSlot);
                if (trafficTime < minTrafficTime)
                {
                    minTrafficTime = trafficTime;
                    bestTimeSlot = time;
                }
            }
            return bestTimeSlot;
        }

        private int FindMedianTime(RouteSlots routeSlots)
        {
            var durationList = routeSlots.durationInMins;
            if (durationList.Count == 0)
            {
                return Int32.MaxValue;
            }
            durationList.Sort();
            if (durationList.Count % 2 != 0)
            {
                int mid = (durationList.Count - 1) / 2;
                return durationList[mid];
            }
            int mid1 = durationList.Count / 2;
            int mid2 = (durationList.Count - 1) / 2;
            return (durationList[mid1] + durationList[mid2]) / 2;
        }

        public async Task InsertAllSlotsForTimeRange(string fromPlaceId, string toPlaceId, int startTime, int endTime)
        {
            await this.semaphoreSlim.WaitAsync();
            try
            {
                var timeSlots = GetAllTimeSlots(startTime, endTime);
                foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
                {
                    var existingRouteSlots = await this.routeSlotRepository.GetSlotData(fromPlaceId, toPlaceId, (int)dayOfWeek);
                    HashSet<int> existingTimeSlots = GetExistingTimeSlots(existingRouteSlots);
                    foreach (int tslot in timeSlots)
                    {
                        if (!existingTimeSlots.Contains(tslot))
                        {
                            await this.routeSlotRepository.InsertSlotIfNotExist(fromPlaceId, toPlaceId, (int)dayOfWeek, tslot);
                        }
                    }
                }
            } finally
            {
                this.semaphoreSlim.Release();
            }
        }

        private HashSet<int> GetExistingTimeSlots(List<RouteSlots> routeSlots)
        {
            HashSet<int> existingSlots = new HashSet<int>();
            foreach(var rSlot in routeSlots)
            {
                existingSlots.Add(rSlot.timeSlot);
            }
            return existingSlots;
        }

        private Dictionary<int, RouteSlots> ConvertToRouteSlotDictionary(List<RouteSlots> routeSlots)
        {
            Dictionary<int, RouteSlots> routeSlotDict = new Dictionary<int, RouteSlots>();
            foreach (var rSlot in routeSlots)
            {
                routeSlotDict.TryAdd(rSlot.timeSlot, rSlot);
            }
            return routeSlotDict;
        }

        private List<int> GetAllTimeSlots(int startTime, int endTime)
        {
            List<int> timeSlots = new List<int>();
            int current = startTime - (startTime % 10);
            if (current < startTime)
            {
                current += 10;
            }
            while (current <= endTime)
            {
                timeSlots.Add(current);
                current += 10;
                if ((current % 100) == 60)
                {
                    current = current - (current % 100);
                    current += 100;
                }
            }
            return timeSlots;
        }
    }
}
