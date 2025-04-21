using DotNetAuth.Domain.Entities;

namespace DotNetAuth.Service
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser User);
        string GenerateRefreshToken();
    }
}
