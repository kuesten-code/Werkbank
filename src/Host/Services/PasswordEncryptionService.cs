using Microsoft.AspNetCore.DataProtection;

namespace Kuestencode.Werkbank.Host.Services;

public class PasswordEncryptionService
{
    private readonly IDataProtector _protector;

    public PasswordEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SmtpPasswordProtection");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch
        {
            // Fallback for legacy/plain text values.
            return cipherText;
        }
    }
}
