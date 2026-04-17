namespace MentoringApp.Service
{
    /// <summary>
    /// Discriminated union for service operation outcomes.
    /// Use <see cref="Ok"/> for success, <see cref="Failure"/> for domain errors,
    /// and <see cref="ValidationFailure"/> when FluentValidation errors are present.
    /// Callers check <see cref="Success"/> before using any data.
    /// </summary>
    public class Result
    {
        public bool Success { get; protected set; }
        public string? ErrorMessage { get; protected set; }
        /// <summary>Field-keyed validation errors populated by <see cref="ValidationFailure"/>.</summary>
        public Dictionary<string, string>? ValidationErrors { get; set; }
        public static Result ValidationFailure(Dictionary<string, string> errors)
            => new Result { Success = false, ValidationErrors = errors, ErrorMessage = "Validation failed." };

        public static Result Ok() => new() { Success = true };
        public static Result Failure(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Data-carrying variant of <see cref="Result"/>.
    /// <see cref="Data"/> is only valid when <see cref="Result.Success"/> is <c>true</c>.
    /// </summary>
    public class Result<T> : Result
    {
        public T? Data { get; private set; }
        public static Result<T> Ok(T data) => new() { Success = true, Data = data };
        public new static Result<T> Failure(string message) => new() { Success = false, ErrorMessage = message };
        public new static Result<T> ValidationFailure(Dictionary<string, string> errors)
            => new Result<T> { Success = false, ValidationErrors = errors, ErrorMessage = "Validation failed." };

    }
}


        