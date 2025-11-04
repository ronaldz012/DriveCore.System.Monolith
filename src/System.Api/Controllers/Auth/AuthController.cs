using System.Api.Result;
using Auth.Dtos.Users;
using Auth.UseCases.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(UserUseCases userUseCases) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserByAdminDto dto)
        {
            return await userUseCases.RegisterUser.Execute(dto, dto.RoleIds)
                                                    .ToValueOrProblemDetails();
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            return await userUseCases.Login.Execute(dto)
                                                    .ToValueOrProblemDetails();
        }
    }
}
 