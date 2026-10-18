using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Options;

namespace ZaphkielProcessor.GitHub;

public sealed class GitHubSignatureVerifier(IOptions<GitHubOptions> options) : IGitHubSignatureVerifier
{
    private const string Prefix = "sha256=";
    private readonly byte[] _secret = Encoding.UTF8.GetBytes(options.Value.WebhookSecret);

    public bool Verify(ReadOnlySpan<byte> body, string? signatureHeader)
    {
        if (signatureHeader is null || !signatureHeader.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signatureHeader.AsSpan(Prefix.Length));
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(_secret, body);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
