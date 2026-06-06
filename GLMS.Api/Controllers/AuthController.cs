// references:
// https://www.rfc-editor.org/info/rfc7515/
// https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler?view=msal-web-dotnet-latest
// https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler.writetoken?view=msal-web-dotnet-latest
// https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.symmetricsecuritykey?view=net-11.0-pp
// https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.signingcredentials?view=netframework-4.8.1
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.symmetricsecuritykey?view=msal-web-dotnet-latest

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GLMS.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GLMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // DANGER: demo credentials - do NOT use in production
        // TODO: remove hardcoded credentials
        var adminUser = _configuration["Auth:AdminUsername"] ?? "admin";
        var adminPass = _configuration["Auth:AdminPassword"] ?? "Admin@1234";

        if (!string.Equals(request.Username, adminUser, StringComparison.OrdinalIgnoreCase) ||
            request.Password != adminPass)
        {
            return Unauthorized("Invalid username or password.");
        }

        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "glms-api",
            audience: _configuration["JwtSettings:Audience"] ?? "glms-web",
            claims: [new Claim(ClaimTypes.Name, request.Username)],
            expires: expires,
            signingCredentials: creds);

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            request.Username,
            expires));
    }
}
