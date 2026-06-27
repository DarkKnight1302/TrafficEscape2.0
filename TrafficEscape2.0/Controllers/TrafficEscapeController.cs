using Microsoft.AspNetCore.Mvc;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrafficEscapeController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IUserService userService;
        private readonly ILogger<TrafficEscapeController> logger;

        public TrafficEscapeController(IAuthorizationService authorizationService, IUserService userService, ILogger<TrafficEscapeController> logger)
        {
            this.authorizationService = authorizationService;
            this.userService = userService;
            this.logger = logger;
        }

        /// <summary>
        /// Returns the saved commute profile for the authenticated user.
        /// </summary>
        [HttpGet]
        [AuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            if (!this.authorizationService.IsValid(userId, HttpContext))
            {
                return Unauthorized();
            }

            try
            {
                var user = await this.userService.GetUser(userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var profile = new UserProfileResponse
                {
                    UserId = user.id,
                    HomePlaceId = user.HomePlaceId,
                    OfficePlaceId = user.OfficePlaceId,
                    HomeLocationName = user.HomeLocationName,
                    OfficeLocationName = user.OfficeLocationName,
                    HomeToOfficeStartTime = user.HomeOffice?.StartTime ?? 0,
                    HomeToOfficeEndTime = user.HomeOffice?.EndTime ?? 0,
                    OfficeToHomeStartTime = user.OfficeHome?.StartTime ?? 0,
                    OfficeToHomeEndTime = user.OfficeHome?.EndTime ?? 0
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to fetch user profile for {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the profile.");
            }
        }
    }
}
