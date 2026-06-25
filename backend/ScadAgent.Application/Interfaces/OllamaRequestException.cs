using System.Net;

namespace ScadAgent.Application.Interfaces;

public class OllamaRequestException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
    public string Model { get; }
    public string BaseUrl { get; }

    public OllamaRequestException(
        HttpStatusCode statusCode,
        string responseBody,
        string model,
        string baseUrl,
        string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        Model = model;
        BaseUrl = baseUrl;
    }
}
