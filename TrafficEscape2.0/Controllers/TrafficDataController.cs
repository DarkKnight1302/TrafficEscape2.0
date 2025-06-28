using Microsoft.AspNetCore.Mvc;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrafficDataController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IUserService userService;

        public TrafficDataController(IAuthorizationService authorizationService, IUserService userService)
        {
            this.authorizationService = authorizationService;
            this.userService = userService;
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
    }
}
