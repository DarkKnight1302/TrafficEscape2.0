namespace TrafficEscape2._0.Models
{
    public class LoginResponse
    {
        public string SessionToken { get; set; }

        public string UserId { get; set; }

        public bool IsUserInitialized { get; set; }
    }
}
