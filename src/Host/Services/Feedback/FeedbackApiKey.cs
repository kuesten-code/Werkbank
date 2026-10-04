using System.Security.Cryptography;
using System.Text;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>
/// API-Keys sind zufällig mit 256 Bit Entropie — ein schneller, ungesalzener SHA-256 reicht
/// daher (kein Wörterbuchangriff möglich) und erlaubt das Nachschlagen per Index.
/// </summary>
public static class FeedbackApiKey
{
    private const string Prefix = "wbfb_";

    public static string Generate() =>
        Prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.Trim()))).ToLowerInvariant();
}
