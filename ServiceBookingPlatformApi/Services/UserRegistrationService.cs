using FieldValidatorAPI;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services.Common;
using static FieldValidatorAPI.CommonFieldValidatorFunctions;
using static FieldValidatorAPI.CommonRegularExpressionValidationPatterns;

namespace ServiceBookingPlatform.Services
{
    public class UserRegistrationService(AppDbContext Db) : IUserRegistrationService
    {
        public async Task<(bool Success, string Message)> RegisterUserAsync(UserDto userDto)
        {
            // Step 1: Validate user input
            var validationResult = ValidateUserDto(userDto);
            if (!validationResult.IsValid)
            {
                return (false, string.Join("; ", validationResult.Errors));
            }

            // Step 2: Check if email already exists
            if (await EmailExistsAsync(userDto.Email))
            {
                return (false, "Email address is already registered");
            }

            // Step 3: Create user entity
            var user = new User
            {
                FirstName = userDto.FirstName.Trim(),
                LastName = userDto.LastName.Trim(),
                Email = userDto.Email.Trim().ToLowerInvariant(),
                PasswordHash = HashPassword(userDto.Password), // Hash the password
                PhoneNumber = userDto.PhoneNumber.Trim(),
                Role = string.IsNullOrWhiteSpace(userDto.Role) ? "Customer" : userDto.Role
            };

            // Step 4: Save to database
            try
            {
                Db.Users.Add(user);
                await Db.SaveChangesAsync();
                return (true, "User registered successfully");
            }
            catch (Exception)
            {
                // Log the exception 
                return (false, "An error occurred during registration");
            }
        }

        public Result ValidateUserDto(UserDto userDto)
        {
            var result = Result.Success("Starting validation");

            // Validate First Name
            if (!RequiredFieldValidDel(userDto.FirstName))
            {
                result.AddError("First name is required");
            }
            else if (!StringLengthFieldValidDel(userDto.FirstName, 2, 50))
            {
                result.AddError("First name must be between 2 and 50 characters");
            }

            // Validate Last Name
            if (!RequiredFieldValidDel(userDto.LastName))
            {
                result.AddError("Last name is required");
            }
            else if (!StringLengthFieldValidDel(userDto.LastName, 2, 50))
            {
                result.AddError("Last name must be between 2 and 50 characters");
            }

            // Validate Email
            if (!RequiredFieldValidDel(userDto.Email))
            {
                result.AddError("Email is required");
            }
            else if (!PatternMatchValidDel(userDto.Email, Email_Address_RegEx_Pattern))
            {
                result.AddError("Email address is not in a valid format");
            }

            // Validate Password
            if (!RequiredFieldValidDel(userDto.Password))
            {
                result.AddError("Password is required");
            }
            else if (!PatternMatchValidDel(userDto.Password, Strong_Password_RegEx_Pattern))
            {
                result.AddError("Password must be 6-10 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
            }

            // Validate Phone Number
            if (!RequiredFieldValidDel(userDto.PhoneNumber))
            {
                result.AddError("Phone number is required");
            }
            else if (!PatternMatchValidDel(userDto.PhoneNumber, Uk_PhoneNumber_RegEx_Pattern))
            {
                result.AddError("Phone number is not in a valid UK format");
            }

            // Validate Role (optional - defaults to Customer)
            if (!string.IsNullOrWhiteSpace(userDto.Role))
            {
                var validRoles = new[] { "Customer", "Staff", "Admin" };
                if (!validRoles.Contains(userDto.Role, StringComparer.OrdinalIgnoreCase))
                {
                    result.AddError("Role must be Customer, Staff, or Admin");
                }
            }

            return result;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await Db.Users.AnyAsync(u => u.Email == email);
        }

        // Password hashing using ASP.NET Core Identity
        private static string HashPassword(string password)
        {
            PasswordHasher<User> passwordHasher = new();
            var passwordHash = passwordHasher.HashPassword(null!, password);
            return passwordHash;
        }
    }
}
