using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using ZaphkielBotCore.Discord;
using ZaphkielBotCore.GitHub;
using ZaphkielBotInfrastructure.Security;

namespace ZaphkielBot.Tests.Infrastructure;

public class SignatureVerifierTests
{
    // ---- GitHub (HMAC-SHA256) ----

    private const string Secret = "0123456789abcdef-secret";
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("""{"action":"created"}""");

    private static GitHubSignatureVerifier GitHub() =>
        new(Options.Create(new GitHubOptions { WebhookSecret = Secret }));

    private static string Sign(byte[] body, string secret) =>
        "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();

    [Fact]
    public void GitHub_accepts_a_valid_signature() =>
        Assert.True(GitHub().Verify(Payload, Sign(Payload, Secret)));

    [Fact]
    public void GitHub_rejects_wrong_secret_tampered_body_and_garbage()
    {
        var verifier = GitHub();

        Assert.False(verifier.Verify(Payload, Sign(Payload, "another-secret-value")));
        Assert.False(verifier.Verify(Encoding.UTF8.GetBytes("tampered"), Sign(Payload, Secret)));
        Assert.False(verifier.Verify(Payload, "sha256=not-hex"));
        Assert.False(verifier.Verify(Payload, "sha1=abcd"));
        Assert.False(verifier.Verify(Payload, null));
    }

    // ---- Discord (Ed25519) ----

    private static (DiscordSignatureVerifier Verifier, Ed25519PrivateKeyParameters Key) Discord()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        var publicKey = ((Ed25519PublicKeyParameters)pair.Public).GetEncoded();

        var verifier = new DiscordSignatureVerifier(Options.Create(new DiscordOptions { PublicKey = Convert.ToHexString(publicKey) }));
        return (verifier, (Ed25519PrivateKeyParameters)pair.Private);
    }

    private static string Sign(Ed25519PrivateKeyParameters key, string timestamp, byte[] body)
    {
        var message = Encoding.UTF8.GetBytes(timestamp).Concat(body).ToArray();
        var signer = new Ed25519Signer();
        signer.Init(true, key);
        signer.BlockUpdate(message, 0, message.Length);
        return Convert.ToHexString(signer.GenerateSignature());
    }

    [Fact]
    public void Discord_accepts_a_valid_signature()
    {
        var (verifier, key) = Discord();

        Assert.True(verifier.Verify("1700000000", Payload, Sign(key, "1700000000", Payload)));
    }

    [Fact]
    public void Discord_rejects_tampered_timestamp_body_and_garbage()
    {
        var (verifier, key) = Discord();
        var signature = Sign(key, "1700000000", Payload);

        Assert.False(verifier.Verify("1700000001", Payload, signature));
        Assert.False(verifier.Verify("1700000000", Encoding.UTF8.GetBytes("tampered"), signature));
        Assert.False(verifier.Verify("1700000000", Payload, "zz"));
        Assert.False(verifier.Verify("1700000000", Payload, "abcd"));
        Assert.False(verifier.Verify(null, Payload, signature));
        Assert.False(verifier.Verify("1700000000", Payload, null));
    }
}
