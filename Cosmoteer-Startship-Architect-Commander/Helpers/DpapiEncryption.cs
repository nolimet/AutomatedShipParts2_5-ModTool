using System.Security.Cryptography;
using System.Text;

namespace ModGenerator.Helpers;

public static class DpapiEncryption
{
    public static byte[] Encrypt(this byte[] plainBytes) =>
        ProtectedData.Protect(
            plainBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser
        );

    public static byte[] Decrypt(this byte[] encryptedBytes) =>
        ProtectedData.Unprotect(
            encryptedBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser
        );

    public static byte[] Encrypt(this MemoryStream stream) => Encrypt(stream.ToArray());

    public static void Decrypt(this MemoryStream stream)
    {
        var decrypted = Decrypt(stream.ToArray());
        stream.SetLength(0);
        stream.Position = 0;
        stream.Write(decrypted, 0, decrypted.Length);
    }

    public static byte[] Encrypt(string plainText) => Encrypt(Encoding.UTF8.GetBytes(plainText));

    public static byte[] Decrypt(string encryptedText) => Decrypt(Convert.FromBase64String(encryptedText));
}
