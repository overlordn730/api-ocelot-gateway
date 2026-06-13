using System.IdentityModel.Tokens.Jwt;
using StackExchange.Redis;

namespace Gateway.Middlewares;

public class TokenValidationMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    ILogger<TokenValidationMiddleware> logger
)
{
    private readonly RequestDelegate _next = next;
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ILogger<TokenValidationMiddleware> _logger = logger;
    private const string BlacklistPrefix = "blacklist:";
    private const string InvalidatedBeforePrefix = "invalidated_before:";

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
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                // 1. Verificar lista negra por JTI
                var isRevoked = await _db.KeyExistsAsync($"{BlacklistPrefix}{jti}");

                if (isRevoked)
                {
                    _logger.LogWarning("Token revocado intentó acceder: {jti}", jti);
                    await RejectAsync(context, "Token revocado");
                    return;
                }

                // 2. Verificar invalidated_before por usuario
                if (userId != null)
                {
                    var invalidatedBeforeValue = await _db.StringGetAsync($"{InvalidatedBeforePrefix}{userId}");

                    if (invalidatedBeforeValue.HasValue)
                    {
                        var invalidatedBefore = long.Parse(invalidatedBeforeValue.ToString());
                        var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;

                        if (iatClaim != null && long.Parse(iatClaim) < invalidatedBefore)
                        {
                            _logger.LogWarning("Token emitido antes de revoke-all para usuario {userId}", userId);
                            await RejectAsync(context, "Sesión revocada, inicie sesión nuevamente");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar token en lista negra");
            }
        }

        await _next(context);
    }

    private static async Task RejectAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            Code = "EXC-401",
            Message = message
        });
    }
}