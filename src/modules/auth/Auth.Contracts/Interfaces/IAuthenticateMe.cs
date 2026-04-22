using System;
using Auth.Contracts.Dtos.Users;
using Shared.Result;

namespace Auth.Contracts.Interfaces;

public interface IAuthenticateMe
{
    Task<Result<SuccessLoginDto>> Execute();

}
