using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace ServiceBookingPlatform.Services
{
    public class RefreshService(AppDbContext Db, IConfiguration config) : IRefreshService
    {
        public async Task<Result<RefreshToken>> GetRefreshToken(UserLogInResponseDto user)
        {
            try
            {
                var storedToken = await Db.RefreshTokens
                .Include(token => token.User)
                .FirstOrDefaultAsync(token => token.Token == user.RefreshToken.ToString());

                if (storedToken == null)
                {
                    return Result<RefreshToken>.Failure("Token not found in database");
                }

                return Result<RefreshToken>.Success(storedToken, "Token found in database");
            }
            catch(Exception ex)
            {
                return Result<RefreshToken>.Failure("An error occurred while retrieving the token", ex.Message);
            }

        }

        public async Task<Result<UserLogInResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            // 1. Find the refresh token in database
            var storedToken = await Db.RefreshTokens
                .Include(token => token.User)
                .FirstOrDefaultAsync(token => token.Token == refreshToken);


            if (storedToken == null)
                return Result<UserLogInResponseDto>.Failure("Invalid refresh token");


            // 2. Validate the token
            if (storedToken.IsRevoked)
                return Result<UserLogInResponseDto>.Failure("Refresh token has been revoked");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return Result<UserLogInResponseDto>.Failure("Refresh token has expired");

            // 3. Revoke the old refresh token (token rotation)
            storedToken.IsRevoked = true;

            // 4. Generate new access token
            var issuer = config["JwtConfig:Issuer"];
            var audience = config["JwtConfig:Audience"];
            var key = config["JwtConfig:Key"];
            var tokenValidityMins = config.GetValue<int>("JwtConfig:TokenValidityMins");
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(tokenValidityMins);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.NameId, storedToken.User!.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, storedToken.User.Email),
                    new Claim(ClaimTypes.Name, storedToken.User.FirstName ?? storedToken.User.Email),
                    new Claim(ClaimTypes.Role, storedToken.User.Role.ToString())
                ]),
                Expires = tokenExpiryTimeStamp,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
                    SecurityAlgorithms.HmacSha256Signature),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            // 5. Generate new refresh token
            var newRefreshToken = new RefreshToken
            {
                UserId = storedToken.User.Id,
                Token = TokenService.GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            Db.RefreshTokens.Add(newRefreshToken);
            await Db.SaveChangesAsync();

            // 6. Return new tokens
            var response = new UserLogInResponseDto(
                accessToken,
                storedToken.User.Email,
                (int)tokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds,
                newRefreshToken.Token
            );

            return Result<UserLogInResponseDto>.Success(response, "Token refreshed successfully");
        }
    }
}
