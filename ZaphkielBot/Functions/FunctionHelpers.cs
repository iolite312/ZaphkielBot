using Microsoft.AspNetCore.Http;

namespace ZaphkielBot.Functions;

internal static class FunctionHelpers
{
    public static async Task<byte[]> ReadBodyAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await req.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
