using System.Api.Middlewares;
using System.Api.Result;
using System.Text;
using Auth.Data;
using Auth.Data.Persistence;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication;
using Auth.UseCases;
using Branches.module;
using Branches.module.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sales API", Version = "v1" });
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = @"JWT Authorization header using the Bearer scheme. <br /> <br />
                      Enter 'Bearer' [space] and then your token in the text input below.<br /> <br />
                      Example: 'Bearer 12345abcdef'<br /> <br />",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });
  c.AddSecurityRequirement(new OpenApiSecurityRequirement()
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,
            },
            new List<string>()
          }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(options =>
{
  // The default scheme for authenticating API requests (JWT)
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

  // The default scheme for challenging unauthenticated users (JWT)
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddGoogle(options =>
{
  options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
  options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
  // Solo para desarrollo - callback URL
  options.CallbackPath = "/api/ExternalAuth/google-login-complete";
}

)
.AddJwtBearer(options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["TokenSettings:Issuer"]!,
    ValidAudience = builder.Configuration["TokenSettings:Audience"]!,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenSettings:SecretKey"]!))
  };
});

// Add DbContext configuration
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthConnection")));

builder.Services.AddDbContext<BranchDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("BranchConnection")));
builder.Services.AddControllers(options =>
{
  options.Filters.Add<ValidationFilter>();
});
// Desactivar el comportamiento automático de [ApiController]
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
  options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddAuthData()
                .AddUseCases()
                .AddInfrastructure(builder.Configuration)
                .AddShared(builder.Configuration)
                .AddBranch(builder.Configuration);


builder.Services.AddCors(options =>
{
  options.AddPolicy("AngularApp", policy =>
  {
    policy.WithOrigins("http://localhost:4200") // El puerto de tu Angular
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Importante para leer cookies si fuera necesario
  });
});

var app = builder.Build();
app.UseCors("AngularApp");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();


