using System.Text;

/// <summary>
/// HTTP message handler that automatically logs all outgoing requests and incoming responses.
/// </summary>
internal class HttpLoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        await LogRequest(request);
        var response = await base.SendAsync(request, cancellationToken);
        await LogResponse(response);
        return response;
    }

    private static async Task LogRequest(HttpRequestMessage request)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== HTTP REQUEST ===");
        sb.AppendLine($"Method: {request.Method}");
        sb.AppendLine($"URL: {request.RequestUri}");

        if (request.Headers.Any())
        {
            sb.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                var value = string.Join(", ", header.Value);
                // Mask Authorization header for security
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    // value = "***MASKED***";
                }
                sb.AppendLine($"  {header.Key}: {value}");
            }
        }

        if (request.Content != null)
        {
            var contentHeaders = request.Content.Headers;
            if (contentHeaders.Any())
            {
                sb.AppendLine("Content Headers:");
                foreach (var header in contentHeaders)
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            if (
                request.Content is StringContent
                || request.Content is FormUrlEncodedContent
                || request.Content is JsonContent
            )
            {
                try
                {
                    var content = await request.Content.ReadAsStringAsync();
                    sb.AppendLine("Body:");
                    sb.AppendLine($"  {content}");
                }
                catch
                {
                    sb.AppendLine("Body: [Unable to read]");
                }
            }
        }

        Console.WriteLine(sb.ToString());
    }

    private static async Task LogResponse(HttpResponseMessage response)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== HTTP RESPONSE ===");
        sb.AppendLine($"Status Code: {response.StatusCode} ({(int)response.StatusCode})");
        sb.AppendLine($"URL: {response.RequestMessage?.RequestUri}");

        if (response.Headers.Any())
        {
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }

        if (response.Content != null && response.Content.Headers.Any())
        {
            sb.AppendLine("Content Headers:");
            foreach (var header in response.Content.Headers)
            {
                sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.Content is not null)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        sb.AppendLine("Body:");
                        sb.AppendLine($"  {content[..Math.Min(500, content.Length)]}");
                        if (content.Length > 500)
                        {
                            sb.AppendLine("  ...[truncated]");
                        }
                    }
                }
                catch
                {
                    sb.AppendLine("Body: [Unable to read]");
                }
            }
        }

        Console.WriteLine(sb.ToString());
    }
}
