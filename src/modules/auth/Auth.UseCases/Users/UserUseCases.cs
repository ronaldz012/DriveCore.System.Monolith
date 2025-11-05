using System;

namespace Auth.UseCases.Users;

public record UserUseCases(RegisterDefaultUser RegisterDefaultUser,
                            RegisterUser RegisterUser,
                             Login Login,
                             VerifyUser VerifyUser,
                             CompletePublicRegister CompletePublicRegister);