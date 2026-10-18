using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ZaphkielCore.Abstractions;
using ZaphkielProcessor.GitHub;
using ZaphkielShared.Options;

namespace ZaphkielTests.Processor;

public class GitHubSignatureVerifierTests
{
    private const string Secret = "0123456789abcdef-secret";
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("""{"action":"created"}""");

    private static IGitHubSignatureVerifier Verifier() =>
        new GitHubSignatureVerifier(Options.Create(new GitHubOptions { WebhookSecret = Secret }));

    private static string Sign(byte[] body, string secret) =>
        "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();

    [Fact]
    public void Accepts_a_valid_signature() =>
        Assert.True(Verifier().Verify(Payload, Sign(Payload, Secret)));

    [Fact]
    public void Rejects_wrong_secret_tampered_body_and_garbage()
    {
        var verifier = Verifier();

        Assert.False(verifier.Verify(Payload, Sign(Payload, "another-secret-value")));
        Assert.False(verifier.Verify(Encoding.UTF8.GetBytes("tampered"), Sign(Payload, Secret)));
        Assert.False(verifier.Verify(Payload, "sha256=not-hex"));
        Assert.False(verifier.Verify(Payload, "sha1=abcd"));
        Assert.False(verifier.Verify(Payload, null));
    }
}
