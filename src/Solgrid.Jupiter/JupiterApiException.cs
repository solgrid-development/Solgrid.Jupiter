using System.Text.Json;

namespace Solgrid.Jupiter;

public sealed class JupiterApiException : Exception
{
    public JupiterApiException(int statusCode, string? error, string? requestId, int? code, string rawBody)
        : base(BuildMessage(statusCode, error, requestId, code))
    {
        StatusCode = statusCode;
        Error = error;
        RequestId = requestId;
        Code = code;
        RawBody = rawBody;
    }

    public int StatusCode { get; }

    public string? Error { get; }

    public string? RequestId { get; }

    public int? Code { get; }

    public string RawBody { get; }

    public static JupiterApiException FromResponse(int statusCode, string body)
    {
        string? error = null;
        string? requestId = null;
        int? code = null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error", out var errorElement) &&
                    errorElement.ValueKind == JsonValueKind.String)
                    error = errorElement.GetString();

                if (doc.RootElement.TryGetProperty("requestId", out var requestIdElement) &&
                    requestIdElement.ValueKind == JsonValueKind.String)
                    requestId = requestIdElement.GetString();

                if (doc.RootElement.TryGetProperty("code", out var codeElement) &&
                    codeElement.ValueKind == JsonValueKind.Number)
                    code = codeElement.GetInt32();
            }
        }
        catch (JsonException)
        {
        }

        return new JupiterApiException(statusCode, error ?? body, requestId, code, body);
    }

    private static string BuildMessage(int statusCode, string? error, string? requestId, int? code)
    {
        var message = $"Jupiter API returned HTTP {statusCode}: {error ?? "unknown error"}";
        if (code.HasValue)
            message += $" (code: {code.Value})";
        if (!string.IsNullOrEmpty(requestId))
            message += $" (requestId: {requestId})";
        return message;
    }
}
