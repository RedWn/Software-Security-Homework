using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.X509;
using Newtonsoft.Json;
using server;


namespace CA
{
    public class CA
    {
        private bool _isRunning;

        private TcpListener _server;
        private List<Client> _clients;

        private string _currentUser;

        public string authorizerPublicKey;
        public string userPublicKey;

        private static Dictionary<string, int> _challengeInputs = new Dictionary<string, int>();

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

        public static string generateCertificate(byte[] publicKey)
        {
            RSA puk = RSA.Create();
            puk.ImportRSAPublicKey(publicKey, out _);
            RSA prk = RSA.Create();
            prk.ImportRSAPrivateKey(publicKey, out _);
            CertificateRequest req = new CertificateRequest("test1", puk, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 certificate = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            // Export the certificate
            return Encoding.UTF8.GetString(certificate.Export(X509ContentType.Pfx, "YourPassword"));
        }

        public static bool checkCertificate(string certificate, RSA publicKey)
        {
            X509Certificate2 x509 = new X509Certificate2(certificate);
            // Get the public key from the certificate
            RSA? certificatePublicKey = x509.GetRSAPublicKey();

            // Compare the public key parameters
            RSAParameters certificateKeyParams = certificatePublicKey.ExportParameters(false);
            RSAParameters givenKeyParams = publicKey.ExportParameters(false);

            // Return true if the modulus and exponent of both keys are the same
            return certificateKeyParams.Modulus.SequenceEqual(givenKeyParams.Modulus) &&
                   certificateKeyParams.Exponent.SequenceEqual(givenKeyParams.Exponent);
        }
        public static void addCI(string username)
        {
            _challengeInputs[username] = new Random().Next(0, 10000);
        }

        public static string getCI(string username)
        {
            return _challengeInputs[username].ToString();
        }

        public static bool passChallenge(int x, string username)
        {
            return (x == challenge(_challengeInputs[username]));
        }

        private static int challenge(int x)
        {
            int ans = (int)(14 * Math.Pow(x, 2) + 5 * x + 3);
            return ans;
        }

        public CA(string ip, int port)
        {
            _clients = new List<Client>();

            _server = new TcpListener(IPAddress.Parse(ip), port);
            _server.Start();
            _isRunning = true;
        }

        public void AcceptConnections()
        {
            while (_isRunning)
            {
                Logger.Log(LogType.info2, "Waiting for connections...");
                Logger.WriteLogs();

                TcpClient newClient = _server.AcceptTcpClient();

                Logger.Log(LogType.info2, $"Client connected from {newClient.Client.LocalEndPoint}");
                Logger.WriteLogs();

                Thread t = new(new ParameterizedThreadStart(HandleClientConnection));
                t.Start(newClient);
            }
        }

        public void HandleClientConnection(object obj)
        {
            Client client = new((TcpClient)obj);
            _clients.Add(client);
            while (client.client.Connected)
            {
                try
                {
                    ReceiveMessage(client);
                }
                catch (Exception e)
                {
                    client.client.Close();
                }
            }
        }

        public void ReceiveMessage(Client client)
        {
            Logger.Log(LogType.warning, $"Message received from {client.port}");
            Logger.WriteLogs();

            string data = client.sReader.ReadLine();

            Package message = Package.FromJSON(data);
            message = client.DecryptPackageBody(message);

            if (message.signature != null && message.body != null && message.body.TryGetValue("role", out string? value) && value == "doctor")
            {
                Console.WriteLine("HELLLLO DOCTORORROROR");
                bool isSignatureVerified = Signer.VerifySignature(client.keys.PGPKeys.PublicKeyRing, JsonConvert.SerializeObject(message.body), message.signature);
                if (!isSignatureVerified)
                {
                    var body = new Dictionary<string, string>
                    {
                        ["message"] = "Invalid signature"
                    };
                    SendMessage(client, new Package("NA", "generic", body));

                    return;
                }
            }
            switch (message.type)
            {
                case "CSR":

                    CA.addCI(_currentUser);
                    client.keys.certificateRSAKey = Encoding.UTF8.GetBytes(message.body["publicKey"]);
                    var body = new Dictionary<string, string>
                    {
                        ["input"] = CA.getCI(_currentUser)
                    };
                    SendMessage(client, new Package("NA", "challenge", body));
                    break;
                case "challenge":
                    if (CA.passChallenge(int.Parse(message.body["key"]), _currentUser))
                    {
                        body = new Dictionary<string, string>
                        {
                            ["certificate"] = CA.generateCertificate(client.keys.certificateRSAKey)
                        };
                        SendMessage(client, new Package("NA", "certificate", body));
                    }
                    else
                    {
                        body = new Dictionary<string, string>
                        {
                            ["massege"] = "Challenge Failed"
                        };
                        SendMessage(client, new Package("NA", "generic", body));
                    }
                    break;
                case "CA":
                    RSA rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(client.keys.certificateRSAKey, out _);
                    if (CA.checkCertificate(message.body["certificate"], rsa))
                    {
                        body = new Dictionary<string, string>
                        {
                            ["massege"] = "Auth Succeed"
                        };
                        SendMessage(client, new Package("NA", "generic", body));
                    }
                    else
                    {
                        body = new Dictionary<string, string>
                        {
                            ["massege"] = "Auth Failed"
                        };
                        SendMessage(client, new Package("NA", "generic", body));
                    }
                    break;
            }
        }

        public void SendMessage(Client client, Package package)
        {
            package = client.EncryptPackageBody(package);
            client.sWriter.WriteLine(JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            //Console.WriteLine("> Sent!");
        }
    }
}