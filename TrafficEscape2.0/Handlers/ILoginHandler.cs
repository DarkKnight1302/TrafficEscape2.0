using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Handlers
{
    public interface ILoginHandler
    {
        public Task<LoginResponse> Login(UserLoginRequest loginRequest);
    }
}
