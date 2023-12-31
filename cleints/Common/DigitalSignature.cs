using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.Text;

namespace server
{
    public class Signer
    {
        public static string SignText(string privateKey, string privateKeyPassword, string inputText)
        {
            PgpSecretKey pgpSec = readSigningSecretKey(privateKey);

            PgpPrivateKey pgpPrivKey = pgpSec.ExtractPrivateKey(privateKeyPassword.ToCharArray());



            PgpSignatureGenerator sGen = new PgpSignatureGenerator(pgpSec.PublicKey.Algorithm, HashAlgorithmTag.Sha1);
            sGen.InitSign(PgpSignature.BinaryDocument, pgpPrivKey);

            Stream inputStream = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
            int ch;
            while ((ch = inputStream.ReadByte()) >= 0)
            {
                sGen.Update((byte)ch);
            }
            PgpSignature signature = sGen.Generate();

            // Convert signature to string
            MemoryStream signatureOut = new MemoryStream();
            ArmoredOutputStream armoredOut = new ArmoredOutputStream(signatureOut);
            signature.Encode(armoredOut);
            armoredOut.Close();



            string signatureString = Encoding.UTF8.GetString(signatureOut.ToArray());
            return signatureString;
        }

        private static PgpSecretKey readSigningSecretKey(string privateKey)
        {
            Stream keyIn = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
            PgpSecretKeyRingBundle pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(keyIn));

            //
            // We just loop through the collection till we find a key suitable for signing.
            // In the real world you would probably want to be a bit smarter about this.
            //
            foreach (PgpSecretKeyRing kRing in pgpSec.GetKeyRings())
            {
                foreach (PgpSecretKey k in kRing.GetSecretKeys())
                {
                    if (k.IsSigningKey)
                    {
                        return k;
                    }
                }
            }
            throw new ArgumentException("Can't find signing key in key ring.");
        }

        public static bool VerifySignature(string publicKey, string inputText, string signatureString)
        {
            PgpPublicKey pgpPubKey = readPublicKey(publicKey);
            PgpSignatureList signatureList = getSignatureList(signatureString);

            PgpSignature signature = signatureList[0];
            signature.InitVerify(pgpPubKey);

            Stream inputStream = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
            int ch;
            while ((ch = inputStream.ReadByte()) >= 0)
            {
                signature.Update((byte)ch);
            }

            return signature.Verify();
        }

        private static PgpPublicKey readPublicKey(string publicKey)
        {
            Stream keyIn = new MemoryStream(Encoding.UTF8.GetBytes(publicKey));
            PgpPublicKeyRingBundle pgpPub = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(keyIn));

            foreach (PgpPublicKeyRing kRing in pgpPub.GetKeyRings())
            {
                foreach (PgpPublicKey k in kRing.GetPublicKeys())
                {
                    if (k.IsEncryptionKey)
                    {
                        return k;
                    }
                }
            }
            throw new ArgumentException("Can't find encryption key in key ring.");
        }

        private static PgpSignatureList getSignatureList(string signatureString)
        {
            Stream signatureIn = new MemoryStream(Encoding.UTF8.GetBytes(signatureString));
            PgpObjectFactory pgpFact = new PgpObjectFactory(PgpUtilities.GetDecoderStream(signatureIn));

            PgpSignatureList p3;
            PgpObject o = pgpFact.NextPgpObject();
            p3 = (PgpSignatureList)o;

            return p3;
        }
    }
}