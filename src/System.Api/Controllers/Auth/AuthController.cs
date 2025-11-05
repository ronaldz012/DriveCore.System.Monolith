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
        [HttpPost("Register/User")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            return await userUseCases.RegisterDefaultUser.Execute(dto)
                                                    .ToValueOrProblemDetails();
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            return await userUseCases.Login.Execute(dto)
                                                    .ToValueOrProblemDetails();
        }
        /// <summary>
        /// this will Active the user
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpPost("VerifyAccount")]
        public async Task<IActionResult> VerifyAccount([FromBody] string code)
        {
            return await userUseCases.VerifyUser.Execute(code)
                                                        .ToValueOrProblemDetails();
        }

    
        [HttpPost]
        public async Task<IActionResult> CompleteUserRole([FromBody] CompleteUserRoleDto dto)
        {
            return await userUseCases.CompletePublicRegister.Execute(dto).ToValueOrProblemDetails();
        }
    }
}
 