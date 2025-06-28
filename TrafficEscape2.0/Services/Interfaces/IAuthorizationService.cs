namespace TrafficEscape2._0.Services.Interfaces
{
    public interface IAuthorizationService
    {
        public bool IsValid(String userId, HttpContext httpContext);
    }
}
