using Gateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// 2. Ocelot — cargar configuración de rutas
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 3. Autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtConfiguration:Issuer"],
            ValidAudience = builder.Configuration["JwtConfiguration:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtConfiguration:Secret"]!))
        };
    });

// 4. Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// 5. Ocelot
builder.Services.AddOcelot();

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ── Pipeline HTTP ──
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Verificar lista negra de Redis
app.UseMiddleware<TokenBlacklistMiddleware>();

// Agregar API Key antes de enrutar
app.Use(async (context, next) =>
{
    context.Request.Headers["X-API-KEY"] =
        builder.Configuration["ApiExchangeKey"]!;
    await next();
});

await app.UseOcelot();

app.Run();