using Microsoft.AspNetCore.Mvc;
using TrafficEscape2._0.Handlers;
using TrafficEscape2._0.Models;
using TrafficEscape2._0.Services.Interfaces;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginHandler loginHandler;
        private readonly IAuthorizationService authorizationService;

        public LoginController(ILoginHandler loginHandler, IAuthorizationService authorizationService)
        {
            this.loginHandler = loginHandler;
            this.authorizationService = authorizationService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginRequest loginRequest)
        {
            LoginResponse response = await this.loginHandler.Login(loginRequest);
            return Ok(response);
        }

        [HttpGet("validateToken")]
        [AuthRequired]
        public IActionResult ValidateToken(String userId)
        {
            bool valid = this.authorizationService.IsValid(userId, HttpContext);
            return Ok(valid);
        }
    }
}
