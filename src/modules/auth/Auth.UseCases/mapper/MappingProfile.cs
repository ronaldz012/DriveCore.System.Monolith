using System;
using Auth.Contracts.Dtos.Features;
using Auth.Contracts.Dtos.Roles;
using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Branches.Contracts.Dtos;
using Mapster;

namespace Auth.UseCases.mapper;

public class MappingConfig: IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateFeatureDto, Feature>()
            .IgnoreNullValues(true);
        
        config.NewConfig<Feature, FeatureDto>();
        config.NewConfig<Feature, FeatureDetailsDto>();

        config.NewConfig<CreateRoleDto, Role>();
        config.NewConfig<RoleFeaturePermissionDto, RoleFeaturePermission>();

        config.NewConfig<Role, RoleDetailsDto>()
        .Map(dest => dest.FeaturePermissions,
         src => src.RoleFeaturePermissions.Select(rmp => new FeaturePermissionsDto
         {
             FeatureId = rmp.FeatureId,
             FeatureName = rmp.Feature.Name,
             CanCreate = rmp.CanCreate,
             CanRead = rmp.CanRead,
             CanUpdate = rmp.CanUpdate,
             CanDelete = rmp.CanDelete
         }).ToList());

        config.NewConfig<RegisterUserDto, User>();

        config.NewConfig<User, UserDetailsDto>();

        config.NewConfig<CreateUserDto, User>();
        
        
        //EXTERNAL MAPPING/////////////////////////
        //config.NewConfig<BranchDto, AvailableBranchesDto>(); //BranchDto es de otro modulo
    }
}
