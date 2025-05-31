using Microsoft.AspNetCore.Mvc;
using TrafficEscape2._0.Handlers;
using TrafficEscape2._0.Models;

namespace TrafficEscape2._0.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginHandler loginHandler;

        public LoginController(ILoginHandler loginHandler)
        {
            this.loginHandler = loginHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginRequest loginRequest)
        {
            LoginResponse response = await this.loginHandler.Login(loginRequest);
            return Ok(response);
        }
    }
}
