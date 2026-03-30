using System.Api.Result;
using Auth.Contracts.Dtos.Users;
using Auth.UseCases.Autentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Authentication | Authorization")]
    public class AuthController(AutenticationUseCases autenticationUseCases) : ControllerBase
    {
        [HttpPost("Register/User")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            return await autenticationUseCases.RegisterDefaultUser.Execute(dto)
                                                    .ToValueOrProblemDetails();
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            return await autenticationUseCases.Login.Execute(dto)
                                                    .ToValueOrProblemDetails();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            return await autenticationUseCases.AutenticateMe.Execute()
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
            return await autenticationUseCases.VerifyUser.Execute(code)
                                                        .ToValueOrProblemDetails();
        }


        [HttpPost("CompleteUser")]
        [Authorize]
        public async Task<IActionResult> CompleteUserRole([FromBody] CompleteUserRoleDto dto)
        {
            return await autenticationUseCases.CompletePublicRegister.Execute(dto).ToValueOrProblemDetails();
        }
        [HttpPost("Google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleAuth([FromBody] string token)
        {
            return await autenticationUseCases.AuthenticateWithGoogle.Execute(token).ToValueOrProblemDetails();
        }
    }
}
 