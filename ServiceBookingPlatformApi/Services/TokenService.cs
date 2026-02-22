using static System.Security.Cryptography.RandomNumberGenerator;
using static System.Convert;
namespace ServiceBookingPlatform.Services
{
    public sealed class TokenService
    {
        public static string GenerateRefreshToken()
        {
            return ToBase64String(GetBytes(64));
        }
    }
}
