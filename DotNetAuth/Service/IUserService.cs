using DotNetAuth.Domain.Constracts;

namespace DotNetAuth.Service
{
    public interface IUserService
    {
        Task<UserResponse> RegisterAsync(UserRegisterRequest request);
        Task<CurrentUserResponse> GetCurrentUserAsync();
        Task<UserResponse> GetByIdAsync(Guid id);
        Task<UserResponse> UpdateAsync(Guid id ,UpdateUserRequest request);
        Task DeleteAsync(Guid id);
        Task<RevokeRefreshTokenRequest> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest);
        Task<CurrentUserResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<UserResponse> LoginAsync(UserLoginRequest request);
    }
}
