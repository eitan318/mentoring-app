namespace MentoringApp.Service
{
    public class Result
    {
        public bool Success { get; protected set; }
        public string? ErrorMessage { get; protected set; }
        public Dictionary<string, string>? ValidationErrors { get; set; }
        public static Result ValidationFailure(Dictionary<string, string> errors) 
            => new Result { Success = false, ValidationErrors = errors, ErrorMessage = "Validation failed." };

        public static Result Ok() => new() { Success = true };
        public static Result Failure(string message) => new() { Success = false, ErrorMessage = message };
    }

    // Inherit for the data-carrying version
    public class Result<T> : Result
    {
        public T? Data { get; private set; }
        public static Result<T> Ok(T data) => new() { Success = true, Data = data };
        public new static Result<T> Failure(string message) => new() { Success = false, ErrorMessage = message };
        public new static Result<T> ValidationFailure(Dictionary<string, string> errors) 
            => new Result<T> { Success = false, ValidationErrors = errors, ErrorMessage = "Validation failed." };

    }
}


        