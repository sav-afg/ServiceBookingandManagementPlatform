using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace ServiceBookingPlatform.Services
{
    public class JwtService(AppDbContext Db, IConfiguration config, IUserLogInService service, ILogger<JwtService> logger) : IJwtService
    {
        public async Task<UserLogInResponseDto?> Authenticate(UserLogInRequestDto request)
        {
            var (success, errors) = await service.ValidateUserCredentialsAsync(request);
            if (!success)
            {
                return null;
            }

            var issuer = config["JwtConfig:Issuer"];
            var audience = config["JwtConfig:Audience"];
            var key = config["JwtConfig:Key"];
            var tokenValidityMins = config.GetValue<int>("JwtConfig:TokenValidityMins");
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(tokenValidityMins);

            var user = await Db.Users.FirstAsync(u => u.Email == request.Email);

            // Clean up old/expired refresh tokens for this user
            var oldTokens = await Db.RefreshTokens
                .Where(t => t.UserId == user.Id &&
                           (t.IsRevoked || t.ExpiresAt < DateTime.UtcNow))
                .ToListAsync();

            if (oldTokens.Count != 0)
            {
                Db.RefreshTokens.RemoveRange(oldTokens);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, request.Email),
                    new Claim(ClaimTypes.Name, user.FirstName),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                ]),
                Expires = tokenExpiryTimeStamp,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
                        SecurityAlgorithms.HmacSha256Signature),
            };

            // Log claims for debugging
            foreach (var claim in tokenDescriptor.Subject.Claims)
            {
                logger.LogInformation("Claim Type: {ClaimType}, Claim Value: {ClaimValue}", claim.Type, claim.Value);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = TokenService.GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            Db.RefreshTokens.Add(refreshToken);
            await Db.SaveChangesAsync();

            return new UserLogInResponseDto(
                accessToken,
                request.Email,
                (int)tokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds,
                refreshToken.Token
            );

        }
    }
}
