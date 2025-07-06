using Microsoft.AspNetCore.Mvc;
using TrafficEscape2._0.Handlers;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrafficDataController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IUserService userService;
        private readonly ITrafficDataHandler trafficDataHandler;

        public TrafficDataController(IAuthorizationService authorizationService, IUserService userService, ITrafficDataHandler trafficDataHandler)
        {
            this.authorizationService = authorizationService;
            this.userService = userService;
            this.trafficDataHandler = trafficDataHandler;
        }

        [HttpGet("processingDays")]
        [AuthRequired]
        public async Task<IActionResult> GetProcessingDays(String userId)
        {
            if (!this.authorizationService.IsValid(userId, HttpContext))
            {
                return Unauthorized();
            }
            int completionDays = await this.userService.GetCompletionDays(userId);
            return Ok(completionDays);
        }

        [HttpGet("duration")]
        [AuthRequired]
        public async Task<IActionResult> GetDurationData(String userId, int dayOfWeek)
        {
            if (!this.authorizationService.IsValid(userId, HttpContext))
            {
                return Unauthorized();
            }
            var response = await this.trafficDataHandler.GetTrafficDataforDay(dayOfWeek, userId);
            return Ok(response);
        }
    }
}
