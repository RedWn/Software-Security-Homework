using Org.BouncyCastle.Bcpg.OpenPgp;
using Safester.CryptoLibrary.Api;
using System.Security.Cryptography;
using System.Text;

public class Coder
{
    public static string encode(string data, string mode, ClientKeys keys)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (mode == "NA")
        {
            return data;
        }

        switch (mode)
        {
            case "NA":
                return data;
            case "AES":
                byte[] plainBytes = Encoding.UTF8.GetBytes(data);
                return AESencode(plainBytes, keys.sessionKey);
            case "PGP":
                return PGPencode(data, keys.targetPublicKeyRing);
            default:
                return "";
        }
    }

    public static string decode(string data, string mode, ClientKeys keys)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (mode == "NA")
        {
            return data;
        }

        switch (mode)
        {
            case "NA":
                return data;
            case "AES":
                byte[] cipherBytes = Convert.FromBase64String(data);
                return AESdecode(cipherBytes, keys.sessionKey);
            case "PGP":
                return PGPdecode(data, keys.PGPKeys.PrivateKeyRing, keys.passphrase);
            default:
                return "";
        }
    }

    public static string AESencode(byte[] plainBytes, byte[] key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
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

    public static string AESdecode(byte[] cipherBytes, byte[] key)
    {
        // Declare the string used to hold the decrypted text
        string plainText = "";

        // Create an Aes object with the specified key
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;

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

    public static string PGPencode(string plainData, string publicKeyRing)
    {
        Encryptor encryptor = new Encryptor();
        List<PgpPublicKey> keys = new List<PgpPublicKey>();
        keys.Add(PgpPublicKeyGetter.ReadPublicKey(publicKeyRing));
        string outText = encryptor.Encrypt(keys, plainData);
        Console.WriteLine("Encryption done.");
        return outText;
    }

    public static string PGPdecode(string cipherData, string privateKeyRing, string passphrase)
    {
        Decryptor decryptor = new Decryptor(privateKeyRing, passphrase.ToArray());
        string decryptedText = decryptor.Decrypt(cipherData);
        Console.WriteLine("Decryption integrity check status: " + decryptor.Verify);
        Console.WriteLine(decryptedText);
        return decryptedText;
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

public class ClientKeys
{
    public PgpKeyPairHolder PGPKeys;
    public byte[] sessionKey;
    public string passphrase;
    public string targetPublicKeyRing;
}