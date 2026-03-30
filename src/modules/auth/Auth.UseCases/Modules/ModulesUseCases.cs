using System;
using Auth.UseCases.Modules;

namespace Auth.UseCases;

public record ModulesUseCases(
    AddModule AddModule,
    GetModule GetModule,
    GetAllModules GetAllModules
);