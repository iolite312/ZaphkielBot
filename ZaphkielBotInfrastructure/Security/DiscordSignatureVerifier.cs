using System.Text;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using ZaphkielBotCore.Abstractions;
using ZaphkielBotCore.Discord;

namespace ZaphkielBotInfrastructure.Security;

/// <summary>Discord signs timestamp + raw body with Ed25519. BouncyCastle is used because .NET has no built-in Ed25519.</summary>
public sealed class DiscordSignatureVerifier : IDiscordSignatureVerifier
{
    private readonly Ed25519PublicKeyParameters _publicKey;

    public DiscordSignatureVerifier(IOptions<DiscordOptions> options) =>
        _publicKey = new Ed25519PublicKeyParameters(Convert.FromHexString(options.Value.PublicKey));

    public bool Verify(string? timestamp, ReadOnlySpan<byte> body, string? signatureHex)
    {
        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signatureHex))
            return false;

        try
        {
            var signature = Convert.FromHexString(signatureHex);
            var timestampBytes = Encoding.UTF8.GetBytes(timestamp);

            var message = new byte[timestampBytes.Length + body.Length];
            timestampBytes.CopyTo(message, 0);
            body.CopyTo(message.AsSpan(timestampBytes.Length));

            var verifier = new Ed25519Signer();
            verifier.Init(false, _publicKey);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or Org.BouncyCastle.Crypto.CryptoException)
        {
            return false; // malformed hex or wrong length: treat as an invalid signature
        }
    }
}
