using System.Api.Filters;
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
using Inventory.Data.Persistence;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Notifications;
using Inventory.UseCases;
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

    // ── Security definition: Bearer + Branch IDs ─────────────────
    c.AddSecurityDefinition("BearerWithBranch", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,   // "ApiKey" permite campo libre
        In   = ParameterLocation.Header,
        Name = "Authorization",             // header real que se envía
        Description =
            "**JWT** — ingrese: `Bearer <token>`\n\n" +
            "**X-Branch-Id** — IDs de sucursal separados por coma (ej: `1,2,3`)\n\n" +
            "Formato combinado en este campo → `Bearer <token> | branches: 1,2,3`\n\n" +
            "> El UI enviará el valor tal cual; use el campo de abajo para los branch IDs."
    });

    // ── Definición separada para X-Branch-Id ────────────────────
    c.AddSecurityDefinition("BranchId", new OpenApiSecurityScheme
    {
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Name        = "X-Branch-Id",
        Description = "IDs de sucursal separados por coma. Ejemplo: `1,2,3`"
    });

    // ── Ambos requeridos globalmente ─────────────────────────────
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "BranchId"
                }
            },
            Array.Empty<string>()
        }
    });

    // ── Bearer estándar (para el candado verde) ──────────────────
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header usando el esquema Bearer.\n\n" +
            "Ingrese **Bearer** [espacio] y luego su token.\n\n" +
            "Ejemplo: `Bearer eyJhbGci...`",
        Name   = "Authorization",
        In     = ParameterLocation.Header,
        Type   = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
builder.Services.AddDbContext<InvDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryConnection")));

builder.Services.AddControllers(options =>
{
  options.Filters.Add<ValidationFilter>();
});
// Desactivar el comportamiento automático de [ApiController]
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
  options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddMemoryCache();
builder.Services.AddAuthData()
                .AddUseCases()
                .AddInfrastructure(builder.Configuration)
                .AddShared(builder.Configuration)
                .AddBranch(builder.Configuration)
                .AddInventory();
//EXTRAER en un DI
builder.Services.AddSignalR();
builder.Services.AddScoped<InventorySignalRStockNotifier>();   // tu notifier
//
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.AllowAnyOrigin()   // Permite solicitudes desde cualquier origen
      .AllowAnyHeader()   // Permite cualquier encabezado
      .AllowAnyMethod();  // Permite cualquier método HTTP
  });
});


var app = builder.Build();
app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
app.MapHub<NotificationHub>("/hubs/notifications");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<BranchMiddleware>();
app.MapControllers();
app.Run();


