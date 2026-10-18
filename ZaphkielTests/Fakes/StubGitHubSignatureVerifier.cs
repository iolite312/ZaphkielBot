using ZaphkielCore.Abstractions;

namespace ZaphkielTests.Fakes;

/// <summary>Answers whatever the test sets (NSubstitute cannot mock ReadOnlySpan parameters).</summary>
public sealed class StubGitHubSignatureVerifier : IGitHubSignatureVerifier
{
    public bool Result { get; set; } = true;

    public bool Verify(ReadOnlySpan<byte> body, string? signatureHeader) => Result;
}
