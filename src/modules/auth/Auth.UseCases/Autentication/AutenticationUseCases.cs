using Auth.Contracts.Interfaces;

namespace Auth.UseCases.Autentication;

public record AutenticationUseCases(RegisterDefaultUser RegisterDefaultUser,
                            RegisterUser RegisterUser,
                             Login Login,
                            IAuthenticateMe AutenticateMe,
                             VerifyUser VerifyUser,
                             CompletePublicRegister CompletePublicRegister,
                             AuthenticateWithGoogle AuthenticateWithGoogle);