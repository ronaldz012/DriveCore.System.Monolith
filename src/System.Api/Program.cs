using System.Api.Middlewares;
using System.Api.Result;
using System.Text;
using Auth.Data;
using Auth.Data.Persistence;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication;
using Auth.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor(); 
builder.Services.AddAuthentication(options =>
{
    // The default scheme for authenticating API requests (JWT)
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    // The default scheme for challenging unauthenticated users (JWT)
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
                .AddShared(builder.Configuration);

var app = builder.Build();
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


