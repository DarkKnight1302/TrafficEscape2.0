using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;
using TrafficEscape2._0.Constants;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ITokenService tokenService;

        public AuthorizationService(ITokenService tokenService)
        {
            this.tokenService = tokenService;    
        }

        public bool IsValid(string userId, HttpContext httpContext)
        {
            string token = ExtractBearerToken(httpContext);
            string audience = ExtractAudience(httpContext);
            if (token == null || audience == null)
            {
                return false;
            }
            ClaimsPrincipal claimsPrincipal = this.tokenService.ValidateToken(token, GlobalConstants.TrafficEscapeServer, audience);
            if (claimsPrincipal == null)
            {
                return false;
            }

            string? userIdFromToken = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId.Equals(userIdFromToken))
            {
                return true;
            }
            return false;
        }

        private string ExtractBearerToken(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("Auth", out var authHeader))
            {
                return authHeader;
            }
            return null;
        }

        private string ExtractAudience(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("Audience", out var audience))
            {
                return audience;
            }
            return null;
        }
    }
}
