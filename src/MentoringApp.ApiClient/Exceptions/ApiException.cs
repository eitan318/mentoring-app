using System.Net;

namespace MentoringApp.ApiClient.Exceptions;

public class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
