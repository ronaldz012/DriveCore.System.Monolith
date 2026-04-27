using System.Api.Result;
using Auth.Contracts.Dtos.Features;
using Auth.UseCases;
using Auth.UseCases.Features;
using Auth.UseCases.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Authentication | Features")]
    public class FeatureController(FeatureUseCases featureUseCases) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddFeature([FromBody] CreateFeatureDto dto)
        {
            return await featureUseCases.CreateFeature.Execute(dto)
            .ToValueOrProblemDetails();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeature(int id)
        {
            return await featureUseCases.GetFeature.Execute(id)
            .ToValueOrProblemDetails();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllFeatures([FromQuery] FeatureQueryDto queryDto)
        {
            return await featureUseCases.ListFeatures.Execute(queryDto)
            .ToValueOrProblemDetails();
        }
    }
}
