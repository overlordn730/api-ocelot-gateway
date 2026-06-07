using System.IdentityModel.Tokens.Jwt;
using StackExchange.Redis;

namespace Gateway;

public class TokenBlacklistMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    ILogger<TokenBlacklistMiddleware> logger
)
{
    private readonly RequestDelegate _next = next;
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ILogger<TokenBlacklistMiddleware> _logger = logger;
    private const string BlacklistPrefix = "blacklist:";

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Replace("Bearer ", "");

            try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var jti = jwtToken.Id;

                var isRevoked = await _db.KeyExistsAsync($"{BlacklistPrefix}{jti}");

                if (isRevoked)
                {
                    _logger.LogWarning("Token revocado intentó acceder: {jti}", jti);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "EXC-401",
                        Message = "Token revocado"
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar token en lista negra");
            }
        }

        await _next(context);
    }
}