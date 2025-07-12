namespace TrafficEscape2._0.Models
{
    [Serializable]
    public class TrafficDataResponse
    {
        public Dictionary<int, int> HomeToOfficeTrafficData { get; set; } = new Dictionary<int, int>();

        public Dictionary<int, int> OfficeToHomeTrafficData { get; set; } = new Dictionary<int, int>();

    }
}
