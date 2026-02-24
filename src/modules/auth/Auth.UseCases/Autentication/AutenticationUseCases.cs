namespace Auth.UseCases.Autentication;

public record AutenticationUseCases(RegisterDefaultUser RegisterDefaultUser,
                            RegisterUser RegisterUser,
                             Login Login,
                            AutenticateMe AutenticateMe,
                             VerifyUser VerifyUser,
                             CompletePublicRegister CompletePublicRegister,
                             AuthenticateWithGoogle AuthenticateWithGoogle);