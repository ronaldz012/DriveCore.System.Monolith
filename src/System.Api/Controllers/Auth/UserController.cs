
using System.Api.Result;
using Auth.Dtos.Users;
using Auth.UseCases.Users;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserUserCases userUserCases) : ControllerBase
    {

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery]UserQueryDto request)
        {
            return await userUserCases.GetAllUsers.execute(request).ToValueOrProblemDetails();
        }
    }
}
 