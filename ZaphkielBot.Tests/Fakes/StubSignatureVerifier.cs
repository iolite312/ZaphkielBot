using ZaphkielBot.Core.Abstractions;

namespace ZaphkielBot.Tests.Fakes;

/// <summary>Stub for both signature verifiers: answers whatever the test sets, so services can be tested without crypto.</summary>
public sealed class StubSignatureVerifier : IDiscordSignatureVerifier, IGitHubSignatureVerifier
{
    public bool Result { get; set; } = true;

    public bool Verify(string? timestamp, ReadOnlySpan<byte> body, string? signatureHex) => Result;

    public bool Verify(ReadOnlySpan<byte> body, string? signatureHeader) => Result;
}
