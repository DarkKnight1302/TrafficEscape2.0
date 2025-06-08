using Microsoft.AspNetCore.Authorization;
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

        [AuthRequired]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUser(
        [FromBody] UserUpdateRequest userUpdateRequest)
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
            }
            else
            {
                return Unauthorized("Invalid token");
            }

            return Ok();
        }


        private string ExtractBearerToken()
        {
            if (HttpContext.Request.Headers.TryGetValue("Auth", out var authHeader))
            {
                return authHeader;
            }
            return null;
        }
    }
}
