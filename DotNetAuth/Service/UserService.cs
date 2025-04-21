using AutoMapper;
using DotNetAuth.Domain.Constracts;
using DotNetAuth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DotNetAuth.Service
{
    public class UserService : IUserService
    {
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<IUserService> _logger;

        public UserService(ITokenService tokenService, ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<IUserService> logger)
        {
            _tokenService = tokenService;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserResponse> RegisterAsync(UserRegisterRequest request)
        {
            _logger.LogInformation("Registering user");
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null) {
                _logger.LogError("Email already exists");
                throw new Exception("Email already exists");
            }
            var newUser = _mapper.Map<ApplicationUser>(request);
            //generate a unique username
            newUser.UserName = GenerateUserName(request.FirstName,request.LastName);
            var result = await _userManager.CreateAsync(newUser,request.Password);
            if (!result.Succeeded) {
                var error = string.Join(".",result.Errors.Select(e=> e.Description));
                _logger.LogError("faild to create User: {error}" , error);
                throw new Exception($"faild to create User {error}");
            }
            _logger.LogInformation("user created Successfully");
            await _tokenService.GenerateToken(newUser);
            return _mapper.Map<UserResponse>(newUser);
        }
        private string GenerateUserName(string FirstName , string LastName)
        {
            var baseUserName = $"{FirstName}{LastName}".ToLower();
            var username = baseUserName;
            var count = 1;
            while (_userManager.Users.Any(u => u.UserName == username)) {
                username = $"{baseUserName}{count}";
                count++;
            }
            return username ;
        }
        public async Task<UserResponse> LoginAsync(UserLoginRequest request)
        {
            if (request == null) {
                _logger.LogError("Login request is null");
                throw new ArgumentNullException(nameof(request));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user,request.Password))
            {
                _logger.LogError("Invalid email or password");
                throw new Exception("Invalid email or password");
            }

            var token = await _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            //hash the refresh token

            using var sha256 = SHA256.Create();
            var refreshTokenHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            user.RefreshToken =Convert.ToBase64String(refreshTokenHash);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            // update user information in the database 
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) 
            {
                var error = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Faild to update user {error}", error);
                throw new Exception($"Faild to update user {error}");
            }
            var userRespons = _mapper.Map<ApplicationUser,UserResponse>(user);
            userRespons.AccessToken = token;
            userRespons.RefreshToken = refreshToken;

            return userRespons;

        }
        public async Task<UserResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting User By Id");
            var user = await _userManager.FindByIdAsync(id.ToString());
            if(user == null)
            {
                _logger.LogError("User Not Found");
                throw new Exception("User Not Found");
            }
            _logger.LogInformation("User Found");
            return _mapper.Map<UserResponse>(user);
        }

        public async Task<CurrentUserResponse> GetCurrentUserAsync()
        {
            var user = await _userManager.FindByIdAsync(_currentUserService.GetUserId());
            if(user == null)
            {
                _logger.LogError("User Not Found");
                throw new Exception("User Not Found");
            }
            return _mapper.Map<CurrentUserResponse>(user);
        }

        public async Task<CurrentUserResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            _logger.LogInformation($"Refresh token request");
            //hash the incomming refresh token and commer it with the one stored in the database
            using var sha256 = SHA256.Create();
            var refreshTokenHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.RefreshToken));
            var hashRefreshToken = Convert.ToBase64String(refreshTokenHash);
            //find the user with the refresh token 

            var user =await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashRefreshToken);

            if(user == null)
            {
                _logger.LogError("Invalid refresh token");
                throw new Exception("Invalid refresh token");
            }

            //token is valid token is not expire
            if(user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                _logger.LogError("REfresh token Expired for user id {userid}",user.Id);
                throw new Exception("REfresh token expired");
            }
            var newAccessToken = await _tokenService.GenerateToken(user);
            _logger.LogInformation("Access token generated Succesfully");
            var currentUserResponce = _mapper.Map<CurrentUserResponse>(user);
            currentUserResponce.AccessToken = newAccessToken;
            return currentUserResponce;
        }

        public async Task<RevokeRefreshTokenRequest> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        {
            _logger.LogInformation("REvokeing refresh token ");

            try
            {
                //hash the incoming refresh token and compare it with the one store in the database 
                using var sha256 = SHA256.Create();
                var refrefreshToken = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenRemoveRequest.RefreshToken));
                var hashedRefreshToken = Convert.ToBase64String(refrefreshToken);

                //find the user base on the refresh token 
                var user = await _userManager.Users.FirstOrDefaultAsync(s => s.RefreshToken == hashedRefreshToken);
                if (user == null) {
                    _logger.LogError("invalid refresh token");
                    throw new Exception("Invalid refresh token ");
                }

                //validate the refresh token expire time 
                if (user.RefreshTokenExpiryTime < DateTime.UtcNow) 
                {
                    _logger.LogWarning("refresh token expire for the user Id {UserId}", user.Id);
                    throw new Exception("refresh token expired");
                }

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                //update user information iin database
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) 
                {
                    _logger.LogError("Faild to update user");
                    {
                        return new RevokeRefreshTokenRequest
                        {
                            Message = "FAild to remove refresh token",
                        };
                    }
                }
                _logger.LogInformation("Refresh toekn revoke succsessfully");
                return new RevokeRefreshTokenRequest
                {
                    Message = "Refresh token revoke succsessfully"
                };
            }
            catch (Exception ex)
            { 
                _logger.LogError("Faild to reovke refresh token  :{ex}",ex.Message);
                throw new Exception("faild to revoke refresh token");
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) {
                _logger.LogError("User Not Found");
                throw new Exception("User Not Found");
            }
            await _userManager.DeleteAsync(user);
        }

        public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) {
                _logger.LogError("user not found");
                throw new Exception("user not found");

            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Gender = request.Gender;

            await _userManager.UpdateAsync(user);
            return _mapper.Map<UserResponse>(user);
        }
    }
}



