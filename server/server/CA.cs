using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.X509;

public class CA
{
    public async Task caServer()
    {
        using (RSA caKey = RSA.Create(2048))
        {
            // The CA's certificate and private key
            // X509Certificate2 caCert = new X509Certificate2(File.ReadAllBytes("ca.cer"));
            // RSA caKey = caCert.GetRSAPrivateKey();

            // The CA's endpoint
            int caPort = 8080;

            // Create a TCP listener
            TcpListener listener = new TcpListener(IPAddress.Any, caPort);
            listener.Start();

            // Accept a TCP client
            TcpClient client = await listener.AcceptTcpClientAsync();

            // Get the network stream
            NetworkStream stream = client.GetStream();

            // Read the CSR from the stream
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            byte[] csrBytes = new byte[bytesRead];
            Array.Copy(buffer, csrBytes, bytesRead);

            //
            // // Read the CSR from a file
            // string csrStringFromFile = File.ReadAllText("mycsr.csr");

            // // Convert the CSR to a byte array
            // byte[] csrBytesFromFile = Convert.FromBase64String(csrStringFromFile);

            // Create a PKCS10 certification request from the CSR
            Pkcs10CertificationRequest csr = new Pkcs10CertificationRequest(csrBytes);

            // Get the subject name
            X509Name subjectName = csr.GetCertificationRequestInfo().Subject;

            // Get the public key
            AsymmetricKeyParameter publicKey = csr.GetPublicKey();

            //

            // Parse the CSR and extract the public key and the information
            // CertificateRequest csr2 = new CertificateRequest(csrBytes);
            // RSA publicKey = csr.PublicKey.Key as RSA;
            // string name = csr2.SubjectName.Name;
            // string email = csr.CertificateExtensions["2.5.29.17"].Format(false); // Subject alternative name extension


            // Verify the digital signature of the CSR using the public key
            bool verified = csr.Verify(publicKey);// VerifySignature(publicKey);

            // Check the information in the CSR
            bool matched = subjectName.ToString().Contains("@damascus.edu");

            // Ask the professor to solve a simple numerical equation
            Random random = new Random();
            int a = random.Next(1, 10);
            int b = random.Next(1, 10);
            int c = a + b;
            string question = $"{a} + {b} = ?";
            byte[] questionBytes = Encoding.UTF8.GetBytes(question);
            await stream.WriteAsync(questionBytes, 0, questionBytes.Length);

            // Wait for the professor's answer
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string answer = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            int d = int.Parse(answer);

            // Check the professor's answer
            bool correct = c == d;

            /*
                        // If the verification steps are successful, create a certificate for the professor
                        if (verified && matched && correct)
                        {
                            // Create a certificate request with the CSR's subject name, public key, hash algorithm, and signature padding
                            CertificateRequest req = new CertificateRequest(subjectName.ToString(), csr.PublicKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                            // Copy the extensions from the CSR
                            foreach (X509Extension ext in csr.CertificateExtensions)
                            {
                                req.CertificateExtensions.Add(ext);
                            }

                            // Create a certificate for the professor, valid for one year, signed by the CA
                            DateTimeOffset now = DateTimeOffset.Now;
                            X509Certificate2 cert = req.Create(new X509Certificate2(), now, now.AddYears(1), Guid.NewGuid().ToByteArray());

                            // Export the certificate as a byte array
                            byte[] certBytes = cert.Export(X509ContentType.Cert);

                            // Send the certificate to the professor
                            await stream.WriteAsync(certBytes, 0, certBytes.Length);
                        }
                        else
                        {
                            // Send an error message to the professor
                            string errorMessage = "Verification failed";
                            byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                            await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
                        }
            */
            // Close the stream and the client
            stream.Close();
            client.Close();
        }
    }
}