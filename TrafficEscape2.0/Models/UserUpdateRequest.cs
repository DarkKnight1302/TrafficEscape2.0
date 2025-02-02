namespace TrafficEscape2._0.Models
{
    public class UserUpdateRequest
    {
        public string UserId { get; set; }

        public string HomePlaceId { get; set; }

        public string OfficePlaceId { get; set; }

        public int HomeToOfficeStartTime { get; set; }

        public int HomeToOfficeEndTime { get; set; }

        public int OfficeToHomeStartTime { get; set; }

        public int OfficeToHomeEndTime { get; set; }

        public string Audience { get; set; }
    }
}
