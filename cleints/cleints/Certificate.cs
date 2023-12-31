using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class Certificate
{
    public async void csrClient()
    {
        using (RSA rsa = RSA.Create(2048))
        {
            // Create a certificate request with the certificate's subject name, public key, hash algorithm, and signature padding
            CertificateRequest req = new CertificateRequest("John@gmail.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Add any extensions that you want to include in the CSR 
            //(This certificate can not issue other certificates)
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

            // Generate the CSR as a byte array
            byte[] csr = req.CreateSigningRequest();

            // Convert the CSR to a base64-encoded string
            string csrString = Convert.ToBase64String(csr);

            // The CA's endpoint
            string caUrl = "https://ca.damascus.edu/sign";

            // Create an HTTP content with the CSR and the additional information
            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("csr", csrString),
            });

            // The HTTP client
            HttpClient client = new HttpClient();

            // Send the request and get the response
            HttpResponseMessage response = await client.PostAsync(caUrl, content);

            // Check the status code
            if (response.IsSuccessStatusCode)
            {
                // Read the response body as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert the response body to a byte array
                byte[] bytes = Convert.FromBase64String(responseBody);

                // Create an X509Certificate2 instance and load the byte array
                X509Certificate2 certificate = new X509Certificate2(bytes);

                // Do something with the certificate
                Console.WriteLine("Certificate received: " + certificate.Subject);

                // Verify the certificate using the base policy
                bool verified = certificate.Verify();

                // Display the verification result
                Console.WriteLine("Certificate verified: " + verified);
            }
        }
    }
}