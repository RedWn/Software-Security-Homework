using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class Coder
{
    public static string encode(string data, byte[] AESKey, string mode)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (mode == "NA")
        {
            return data;
        }
        byte[] plainBytes = Encoding.UTF8.GetBytes(data);

        switch (mode)
        {
            case "NA":
                return data;
            case "AES":
                return AESencode(plainBytes, AESKey);
            /*case "PGP":
                return PGPencode(plainBytes, key);*/
            default:
                return "";
        }
    }

    public static string decode(string data, byte[] AESKey, string mode)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (mode == "NA")
        {
            return data;
        }
        byte[] cipherBytes = Convert.FromBase64String(data); ;

        switch (mode)
        {
            case "NA":
                return data;
            case "AES":
                return AESdecode(cipherBytes, AESKey);
            /*case "PGP":
                return PGPdecode(cipherBytes, key);*/
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
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;

            // Get the initialization vector from the encrypted data
            byte[] temp = new byte[aes.BlockSize / 8];
            Array.Copy(cipherBytes, 0, temp, 0, temp.Length);
            aes.IV = temp;

            // Create a decryptor to perform the stream transform
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            // Create the streams used for decryption
            using (MemoryStream msDecrypt = new MemoryStream(cipherBytes, aes.IV.Length, cipherBytes.Length - aes.IV.Length))
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
