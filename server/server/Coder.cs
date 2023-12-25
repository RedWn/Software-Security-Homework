using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class Coder
{
    public enum Mode { AESsecretKey, }

    public static string encode(string data, byte[] key, Mode mode) {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");

        byte[] plainBytes = Encoding.UTF8.GetBytes(data);

        switch(mode){
            case Mode.AESsecretKey:
            return AES(plainBytes, key);
            default:
                return "";
        }
    }
    public static string AES(byte[] plainBytes, byte[] keyBytes) {
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.GenerateIV();

            // Create an encryptor to perform the stream transform
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                // Create a memory stream to store the encrypted bytes
                using (MemoryStream ms = new MemoryStream())
                {
                    // Prepend the IV to the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    // Create a crypto stream to perform the encryption
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Write the plain bytes to the stream
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        // Flush the final block of data
                        cs.FlushFinalBlock();
                        // Return the encrypted bytes as a base64-encoded string
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }
    }

    public static byte[] getSessionKey() {
        using (Aes aes = Aes.Create()) {
            aes.GenerateKey();
            return aes.Key;
        }
    }
}
