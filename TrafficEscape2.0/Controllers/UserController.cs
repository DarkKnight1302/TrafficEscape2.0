using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;
using TrafficEscape2._0.Constants;
using TrafficEscape2._0.Handlers;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly ILogger<UserController> logger;
        private readonly ITokenService tokenService;

        public UserController(IUserService userService, ILogger<UserController> logger, ITokenService tokenService)
        {
            this.userService = userService;
            this.logger = logger;
            this.tokenService = tokenService;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(UserUpdateRequest userUpdateRequest)
        {
            string token = ExtractBearerToken();
            if (token == null)
            {
                return Unauthorized("No valid bearer token found.");
            }
            ClaimsPrincipal claimsPrincipal = this.tokenService.ValidateToken(token, GlobalConstants.TrafficEscapeServer, userUpdateRequest.Audience);
            if (claimsPrincipal == null)
            {
                return Unauthorized("Invalid token");
            }

            string? userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userUpdateRequest.UserId.Equals(userId))
            {
                this.logger.LogInformation("Updating user");
                await this.userService.UpdateUser(userUpdateRequest);
            } else
            {
                return Unauthorized("Invalid token");
            }

            return Ok();
        }

        private string ExtractBearerToken()
        {
            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Step 2: Extract the token from the header
                var bearerToken = authHeader.FirstOrDefault();

                if (!string.IsNullOrEmpty(bearerToken) && bearerToken.StartsWith("Bearer "))
                {
                    // Step 3: Parse the token (remove "Bearer " prefix)
                    var token = bearerToken.Substring("Bearer ".Length).Trim();

                    return token;
                }
            }
            return null;
        }
    }
}
