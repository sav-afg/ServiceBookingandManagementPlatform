namespace ServiceBookingPlatform.Services.Common
{
    /// <summary>
    /// Non-generic result for validation and operations without data payload.
    /// Can replace FieldValidatorAPI.ValidationResult
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }
        public List<string> Errors { get; private set; }

        private Result(bool isSuccess, string message, List<string>? errors = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Errors = errors ?? [];
        }

        public static Result Success(string message = "Validation successful")
            => new(true, message);

        public static Result Failure(string message, List<string>? errors = null)
            => new(false, message, errors);

        public static Result Failure(string message, params string[] errors)
            => new(false, message, [.. errors]);

        /// <summary>
        /// Add an error to the result (for progressive validation)
        /// </summary>
        public void AddError(string error)
        {
            IsSuccess = false;
            Errors.Add(error);
        }

        /// <summary>
        /// Alias for IsSuccess to match ValidationResult API
        /// </summary>
        public bool IsValid => IsSuccess;
    }

    /// <summary>
    /// Generic result for operations that return data
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string Message { get; }
        public List<string> Errors { get; }

        private Result(bool isSuccess, T? data, string message, List<string>? errors = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Errors = errors ?? [];
        }

        public static Result<T> Success(T data, string message = "Operation successful")
            => new(true, data, message);

        public static Result<T> Failure(string message, List<string>? errors = null)
            => new(false, default, message, errors);

        public static Result<T> Failure(string message, params string[] errors)
            => new(false, default, message, [.. errors]);

        /// <summary>
        /// Alias for IsSuccess to match ValidationResult API
        /// </summary>
        public bool IsValid => IsSuccess;
    }
}