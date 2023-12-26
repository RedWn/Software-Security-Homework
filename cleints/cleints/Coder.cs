using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class Coder
{
    public enum Mode { AESsecretKey, }

    public static string encode(string data, byte[] key, Mode mode)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");

        byte[] plainBytes = Encoding.UTF8.GetBytes(data);

        switch (mode)
        {
            case Mode.AESsecretKey:
                return AESencode(plainBytes, key);
            default:
                return "";
        }
    }

    public static string decode(string data, byte[] key, Mode mode)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");

        byte[] cipherBytes = Convert.FromBase64String(data); ;

        switch (mode)
        {
            case Mode.AESsecretKey:
                return AESdecode(cipherBytes, key);
            default:
                return "";
        }
    }

    public static string AESencode(byte[] plainBytes, byte[] keyBytes)
    {
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

    public static string AESdecode(byte[] cipherBytes, byte[] keyBytes)
    {
        // Declare the string used to hold the decrypted text
        string plainText = null;

        // Create an Aes object with the specified key
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = keyBytes;

            // Get the initialization vector from the encrypted data
            aesAlg.IV = new byte[aesAlg.BlockSize / 8];
            Array.Copy(cipherBytes, 0, aesAlg.IV, 0, aesAlg.IV.Length);

            // Create a decryptor to perform the stream transform
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption
            using (MemoryStream msDecrypt = new MemoryStream(cipherBytes, aesAlg.IV.Length, cipherBytes.Length - aesAlg.IV.Length))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the stream and place them in a string
                        plainText = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        // Return the decrypted string
        return plainText;
    }
    public static byte[] getSessionKey()
    {
        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            return aes.Key;
        }
    }
}
