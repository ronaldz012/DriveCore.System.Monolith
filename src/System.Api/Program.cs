using System.Api.Middlewares;
using System.Api.Result;
using System.Text;
using System.Threading.RateLimiting;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Common.Contracts.Seeder;
using Module.Auth;
using Module.Inventory;
using Module.Sales;
using System.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
  options.AddDocumentTransformer((document, context, cancellationToken) =>
  {
    document.Info = new()
    {
      Title = "Sales API",
      Version = "v1"
    };
    document.Servers =
    [
        new() { Url = "https://localhost:5253" },
        new() { Url = "http://localhost:5264" }
    ];

    // 1. Definir el esquema de seguridad para el Token JWT
    document.Components ??= new Microsoft.OpenApi.Models.OpenApiComponents();
    document.Components.SecuritySchemes.Add("Bearer", new()
    {
      Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
      Scheme = "Bearer",
      BearerFormat = "JWT",
      Description = "Introduce tu token JWT sin la palabra 'Bearer'"
    });

    // 2. Definir el esquema de seguridad para el X-Branch-Id
    document.Components.SecuritySchemes.Add("BranchId", new()
    {
      Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
      In = Microsoft.OpenApi.Models.ParameterLocation.Header,
      Name = "X-Branch-Id",
      Description = "IDs de sucursal separados por coma (ej: 1,2,3)"
    });

    // 3. Aplicar ambos de forma global a todos los endpoints
    var requirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
      {
        new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
      },
      {
        new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "BranchId" } },
        Array.Empty<string>()
      }
    };
    document.SecurityRequirements.Add(requirement);

    return Task.CompletedTask;
  });
});

builder.Services.AddHttpContextAccessor();
var clerkIssuer = builder.Configuration["Clerk:Issuer"]!;
var authorizedParties = builder.Configuration
    .GetSection("Clerk:AuthorizedParties").Get<string[]>() ?? [];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = clerkIssuer;     // descarga el JWKS y rota las claves solo
        options.MapInboundClaims = false;    // conserva "sub" y "email" con su nombre real

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = clerkIssuer,
            ValidateAudience = false,        // Clerk no pone aud por defecto
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(15), // el token dura 60 s: 2 min de margen lo triplicaría
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var azp = ctx.Principal?.FindFirst("azp")?.Value;
                if (azp is null || !authorizedParties.Contains(azp))
                    ctx.Fail("Origen no autorizado.");
                return Task.CompletedTask;
            },
        };
    });









builder.Services.AddCommon(builder.Configuration);

  builder.Services.AuthDependencyInjection(builder.Configuration);

  builder.Services.AddAppInfrastructure();
  builder.Services.AddInventory();
  builder.Services.AddSales();

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
//EXTRAER en un DI
builder.Services.AddSignalR();
//
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            StatusCode = 429,
            Title = "TooManyRequests",
            Detail = "Demasiadas peticiones. Intente nuevamente en unos segundos."
        }, cancellationToken: ct);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var isApiKey = ctx.Request.Headers.ContainsKey("X-Api-Key");
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = isApiKey ? $"{ip}:admin" : ip;
        var permitLimit = isApiKey ? 10 : 60;
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(60),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.SetIsOriginAllowed(_ => true)
      .AllowCredentials()
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
  var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
  var seederSettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Common.Services.SeederSettings>>().Value;
  if (seederSettings.Enabled)
  {
    logger.LogInformation("Seeder enabled (AdminEmail={AdminEmail}) - running DatabaseSeeder...", seederSettings.AdminEmail);
    var seeder = scope.ServiceProvider
      .GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAllAsync();
    logger.LogInformation("Database seeding completed.");
  }
  else
  {
    logger.LogInformation("Seeder disabled - skipping DatabaseSeeder (Seeder:Enabled=false).");
  }
}

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.

//app.MapHub<NotificationHub>("/hubs/notifications");
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        var token = context.Request.Cookies["accessToken"];
        if (!string.IsNullOrEmpty(token))
            context.Request.Headers["Authorization"] = $"Bearer {token}";
    }
    await next(context);
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi(); // Mapea el JSON (/openapi/v1.json)
    
  app.MapScalarApiReference(options =>
  {
    options
      .WithTitle("Sales API")
      .WithTheme(ScalarTheme.Purple)
      .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
  });
}
app.MapControllers();
app.Run();


