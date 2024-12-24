namespace TrafficEscape2._0.Services.Interfaces
{
    public interface IGoogleAuthService
    {
        public Task<string> ValidateAndReturnUser(string idToken);
    }
}
