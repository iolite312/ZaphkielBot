namespace ZaphkielBot.Core.Abstractions;

/// <summary>Verifies the Ed25519 signature Discord attaches to every interaction request.</summary>
public interface IDiscordSignatureVerifier
{
    bool Verify(string? timestamp, ReadOnlySpan<byte> body, string? signatureHex);
}

/// <summary>Verifies the HMAC-SHA256 signature (X-Hub-Signature-256) GitHub attaches to every webhook delivery.</summary>
public interface IGitHubSignatureVerifier
{
    bool Verify(ReadOnlySpan<byte> body, string? signatureHeader);
}
