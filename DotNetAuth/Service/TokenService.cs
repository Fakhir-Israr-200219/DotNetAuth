using DotNetAuth.Domain.Constracts;
using DotNetAuth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DotNetAuth.Service
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _SecurityKey;
        private readonly string? _validIssuer;
        private readonly string? _validAudience;
        private readonly double _expires;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration,ILogger<TokenService> logger,UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key))
            {
                throw new InvalidOperationException("jwt secret key is not configured.");
            }
            _SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            _validIssuer = jwtSettings.ValidIssuer;
            _validAudience = jwtSettings.ValidAudience;
            _expires = jwtSettings.Expires;
        }

        public async Task<string> GenerateToken(ApplicationUser User)
        {
            var signingCredantials = new SigningCredentials(_SecurityKey,SecurityAlgorithms.HmacSha256);
            var claims = await GetClaimsAsync(User);
            var tokenOptions = GenerateTokenOptions(signingCredantials, claims);
            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var refreshToken = Convert.ToBase64String(randomNumber);
            return refreshToken;
        }
        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials ,List<Claim> claims)
        {
            return new JwtSecurityToken(
                issuer: _validIssuer,
                audience:_validAudience,
                claims:claims,
                expires:DateTime.Now.AddMinutes(_expires),
                signingCredentials:signingCredentials);
        }

        private async Task<List<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name , user?.UserName ??  string.Empty),
                new Claim(ClaimTypes.NameIdentifier ,user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FirstName",user.FirstName),
                new Claim("LastName",user.LastName),
                new Claim("Gender",user.Gender),
            };
            var roles = await _userManager.GetRolesAsync(user);
            return claims;
        }
       
    }
}
