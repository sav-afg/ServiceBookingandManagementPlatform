namespace ServiceBookingPlatform.Models.Dtos.User
{
    public record UserLogInResponseDto(
        string AccessToken,
        string Email,
        int ExpiresIn,
        string RefreshToken
    );
}
