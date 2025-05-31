using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;
using TrafficEscape2._0.Constants;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Handlers
{
    public class LoginHandler : ILoginHandler
    {
        private readonly IGoogleAuthService googleAuthService;
        private readonly IUserService userService;
        private readonly ITokenService tokenService;

        public LoginHandler(IGoogleAuthService googleAuthService, IUserService userService, ITokenService tokenService)
        {
            this.googleAuthService = googleAuthService;
            this.userService = userService;
            this.tokenService = tokenService;
        }

        public async Task<LoginResponse> Login(UserLoginRequest loginRequest)
        {
            string userId = await this.googleAuthService.ValidateAndReturnUser(loginRequest.IdToken).ConfigureAwait(false);
            await this.userService.RegisterUser(userId);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            string token = this.tokenService.GenerateToken(claims, GlobalConstants.TrafficEscapeServer, loginRequest.Audience, GlobalConstants.TokenExpiryDays);
            LoginResponse loginResponse = new LoginResponse()
            {
                SessionToken = token,
                UserId = userId
            };
            return loginResponse;
        }
    }
}
