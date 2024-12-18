namespace TrafficEscape2._0.Entities
{
    public class RouteSlots
    {
        public string id { get; set; }
        public string fromPlaceId { get; set; }

        public string toPlaceId { get; set; }

        public int dayOfWeek { get; set;} // Partition key

        public int timeSlot { get; set; }

        public string uid { get; set; }

        public List<int> durationInMins = new List<int>();

        public DateTimeOffset updatedAt { get; set; }
    }
}
