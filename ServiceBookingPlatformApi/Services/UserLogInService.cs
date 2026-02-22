using FieldValidatorAPI;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using static FieldValidatorAPI.CommonFieldValidatorFunctions;
using static FieldValidatorAPI.CommonRegularExpressionValidationPatterns;

namespace ServiceBookingPlatform.Services
{
    public class UserLogInService(AppDbContext Db) : IUserLogInService
    {
        // Password hasher instance
        private static readonly PasswordHasher<User> _passwordHasher = new();

        // Validate user credentials
        public async Task<(bool success, string errors)> ValidateUserCredentialsAsync(UserLogInRequestDto userDto)
        {
            
            var validationResult = ValidateUserDto(userDto);

            // Returns false if there are format errors in the email or password
            if (!validationResult.IsValid)
            {
                return (false, string.Join("; ", validationResult.Errors));
            }

            // Find user by email
            var user = await Db.Users
                .FirstOrDefaultAsync(u => u.Email == userDto.Email);

            // User doesn't exist
            if (user == null)
            {
                return (false, "User not found in database");
            }

            // Verify password
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user, 
                user.PasswordHash, 
                userDto.Password
            );

            return verificationResult == PasswordVerificationResult.Success ? (true, "Log in Successful") : (false, "Password is incorrect");
        }

        //Validates the format of the email and password
        public ValidationResult ValidateUserDto(UserLogInRequestDto user)
        {
            var result = new ValidationResult { IsValid = true };
            var normalizedEmail = user.Email.Trim().ToLowerInvariant();

            // Validate Email
            if (!RequiredFieldValidDel(user.Email))
            {
                result.AddError("Email is required");
            }
            else if (!PatternMatchValidDel(user.Email, Email_Address_RegEx_Pattern))
            {
                result.AddError("Email address is not in a valid format");
            }

            // Validate Password
            if (!RequiredFieldValidDel(user.Password))
            {
                result.AddError("Password is required");
            }
            else if (!PatternMatchValidDel(user.Password, Strong_Password_RegEx_Pattern))
            {
                result.AddError("Password must be 6-10 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
            }

            return result;

        }

        public async Task<(bool success, string message)> LogOutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return (false, "Refresh token is required");
            }

            var token = await Db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
            {
                return (false, "Invalid refresh token");
            }

            if (token.IsRevoked)
            {
                return (false, "Token is already revoked");
            }

            token.IsRevoked = true;
            await Db.SaveChangesAsync();

            return (true, "Logged out successfully");
        }
    }
}
