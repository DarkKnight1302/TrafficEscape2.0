namespace TrafficEscape2._0.Entities
{
    public class User
    {
        public string id { get; set; }

        public string HomePlaceId { get; set;}

        public string OfficePlaceId { get; set; }

        public TimeRange HomeOffice { get; set; }

        public TimeRange OfficeHome { get; set; }

        public int BestTimeHomeOffice { get; set; }

        public int BestTimeOfficeHome { get; set; }

        public DateTimeOffset AnalysisStartTime { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class TimeRange
    {
        public int StartTime { get; set; }

        public int EndTime { get; set; }
    }
}
