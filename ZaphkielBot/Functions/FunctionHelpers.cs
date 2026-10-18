using Microsoft.AspNetCore.Http;

namespace ZaphkielBot.Functions;

internal static class FunctionHelpers
{
    /// <summary>Signatures are computed over the exact bytes sent, so the body is read raw and never re-serialized.</summary>
    public static async Task<byte[]> ReadBodyAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await req.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
