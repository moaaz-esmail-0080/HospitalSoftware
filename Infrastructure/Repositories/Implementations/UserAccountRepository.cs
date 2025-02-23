using BaseLibrary.DTOs;
using BaseLibrary.Entites;
using BaseLibrary.Responses;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = BaseLibrary.Entites.ApplicationUser;
using SystemRoles = BaseLibrary.Entites.SystemRole;
using UserRoles = BaseLibrary.Entites.UserRole;
using AppContext = Infrastructure.Data.AppContext;

namespace Infrastructure.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppContext appContext) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user is null) return new GeneralResponse(false, "Model is empty");

            var checkUser = await FindUserByEmail(user.Email);
            if (checkUser != null) return new GeneralResponse(false, "User registered already");

            // Save user
            var applicationUser = await AddToDatabase(new ApplicationUser
            {
                Fullname = user.Fullname,
                Email = user.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            // Check, Create, and Assign Role
            var checkAdminRole = await appContext.SystemRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == Constants.Admin);
            if (checkAdminRole == null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole { Name = Constants.Admin });
                await AddToDatabase(new UserRole { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "Account Created");
            }

            var checkUserRole = await appContext.SystemRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == Constants.User);
            SystemRole response;
            if (checkUserRole == null)
            {
                response = await AddToDatabase(new SystemRole { Name = Constants.User });
                await AddToDatabase(new UserRole { RoleId = response.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRole { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }
            return new GeneralResponse(true, "Account created");
        }

        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user is null) return new LoginResponse(false, "Model is empty");

            var applicationUser = await FindUserByEmail(user.Email);
            if (applicationUser is null) return new LoginResponse(false, "User not found");

            // Verify Password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Email/Password not valid");

            var getUserRole = await FindUserRole(applicationUser.Id);
            if (getUserRole is null) return new LoginResponse(false, "User role not found");

            var getRoleName = await FindRoleName(getUserRole.RoleId);
            if (getRoleName is null) return new LoginResponse(false, "User role not found");

            string jwtToken = GenerateToken(applicationUser, getRoleName.Name);
            string refreshToken = GenerateRefreshToken();

            // Save Refresh Token to Database
            var findUser = await appContext.RefreshTokenInfos.FirstOrDefaultAsync(t => t.UserId == applicationUser.Id);
            if (findUser is not null)
            {
                findUser.Token = refreshToken;
                await appContext.SaveChangesAsync();
            }
            else
            {
                await AddToDatabase(new RefreshTokenInfo { Token = refreshToken, UserId = applicationUser.Id });
            }

            return new LoginResponse(true, "Login Successfully", jwtToken, refreshToken);
        }

        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(config.Value.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role ?? ""),
            };

            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.UtcNow.AddMinutes(30), // Adjusted expiration time
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRole?> FindUserRole(int userId) =>
            await appContext.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);

        private async Task<SystemRole?> FindRoleName(int roleId) =>
            await appContext.SystemRoles.AsNoTracking().FirstOrDefaultAsync(sr => sr.Id == roleId);

        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(62));

        private async Task<ApplicationUser?> FindUserByEmail(string email) =>
            await appContext.ApplicationUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        private async Task<T> AddToDatabase<T>(T model) where T : class
        {
            var result = await appContext.AddAsync(model);
            await appContext.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            if (token is null) return new LoginResponse(false, "Model is empty");

            var findToken = await appContext.RefreshTokenInfos.FirstOrDefaultAsync(rt => rt.Token == token.Token);
            if (findToken is null) return new LoginResponse(false, "Refresh token not found");

            // Get user details
            var user = await appContext.ApplicationUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == findToken.UserId);
            if (user is null) return new LoginResponse(false, "User not found for refresh token");

            var userRole = await FindUserRole(user.Id);
            if (userRole is null) return new LoginResponse(false, "User role not found");

            var roleName = await FindRoleName(userRole.RoleId);
            if (roleName is null) return new LoginResponse(false, "User role name not found");

            string jwtToken = GenerateToken(user, roleName.Name);
            string refreshToken = GenerateRefreshToken();

            // Update Refresh Token
            findToken.Token = refreshToken;
            await appContext.SaveChangesAsync();

            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);
        }
    }
}
