using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Azure_REST_API_Authorization_HMAC
{
    internal static class Program
    {
        static readonly string AccountName = "YOURACCOUNTNAME";
        static readonly string BaseURL = "https://YOURACCOUNTNAME.REGION.batch.azure.com";
        static readonly string AccountKey = "SHAREDKEY";

        static readonly string RestEndpoint = "jobs";
        static readonly string APIVersion = "2020-03-01.11.0";

        static readonly string CanonicalHeaderIdentifier = "ocp-";

        static readonly HttpClient client = new HttpClient();

        private static void Main()
        {
            var request = BuildRequest(AccountName, AccountKey);

            var response = SendRequest(request, CancellationToken.None).GetAwaiter().GetResult();

            Console.WriteLine(response);
            Console.ReadLine();
        }

        private static HttpRequestMessage BuildRequest(string accountName, string accountKey)
        {
            // Time reference
            var utcNow = DateTime.UtcNow;

            // REST Endpoint
            string uri = string.Format(BaseURL + "/" + RestEndpoint + "?api-version=" + APIVersion);

            // Payload is null, specify any payload on your demand
            byte[] requestPayload = null;

            // Create request
            var httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload)
                };

            // Add headers to the request
            httpRequestMessage.Headers.Add("ocp-date", utcNow.ToString("R", CultureInfo.InvariantCulture));
            httpRequestMessage.Headers.Add("client-request-id", Guid.NewGuid().ToString());
            httpRequestMessage.Headers.Add("return-client-request-id", false.ToString());

            // If you need any additional headers, add them here before creating
            //   the authorization header. 

            // Add the authorization header.
            httpRequestMessage.Headers.Authorization = GetAuthorizationHeader(
               accountName, accountKey, httpRequestMessage);

            return httpRequestMessage;
        }

        private static async Task<string> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Send the request.
            HttpResponseMessage httpResponseMessage = await client.SendAsync(request, cancellationToken);

            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                return await httpResponseMessage.Content.ReadAsStringAsync();
            }
            else
            {
                return "Response status code: " + httpResponseMessage.StatusCode.ToString();
            }
        }

        internal static AuthenticationHeaderValue GetAuthorizationHeader(string accountName, string accountKey, HttpRequestMessage httpRequestMessage, string ifMatch = "", string md5 = "")
        {
            // This is the raw representation of the message signature:

            /* 
                VERB \n
                Content-Encoding \n
                Content-Language \n
                Content-Length \n
                Content-MD5 \n
                Content-Type \n
                Date \n
                If-Modified-Since \n
                If-Match \n
                If-None-Match \n
                If-Unmodified-Since \n
                Range \n
                CanonicalizedHeaders \n
                CanonicalizedResource;
            */

            HttpMethod method = httpRequestMessage.Method;
            string MessageSignature = string.Format("{0}\n\n\n{1}\n{5}\n\n\n\n{2}\n\n\n\n{3}{4}",
                      method.ToString(),
                      httpRequestMessage.Content?.Headers.ContentLength.ToString(),
                      ifMatch,
                      GetCanonicalizedHeaders(httpRequestMessage),
                      GetCanonicalizedResource(httpRequestMessage.RequestUri, accountName),
                      md5);

            // Now turn it into a byte array.
            byte[] SignatureBytes = Encoding.UTF8.GetBytes(MessageSignature);

            // Create the HMACSHA256 version of the storage key.
            HMACSHA256 SHA256 = new HMACSHA256(Convert.FromBase64String(accountKey));

            // Compute the hash of the SignatureBytes and convert it to a base64 string.
            string signature = Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));

            // This is the actual header that will be added to the list of request headers.
            return new AuthenticationHeaderValue("SharedKey", accountName + ":" + signature);
        }

        private static string GetCanonicalizedHeaders(HttpRequestMessage httpRequestMessage)
        {
            var headers = from kvp in httpRequestMessage.Headers
                          where kvp.Key.StartsWith(CanonicalHeaderIdentifier, StringComparison.OrdinalIgnoreCase)
                          orderby kvp.Key
                          select new { Key = kvp.Key.ToLowerInvariant(), kvp.Value };

            StringBuilder sb = new StringBuilder();

            // Create the string in the right format; this is what makes the headers "canonicalized"
            // it means put in a standard format. http://en.wikipedia.org/wiki/Canonicalization
            foreach (var kvp in headers)
            {
                StringBuilder headerBuilder = new StringBuilder(kvp.Key);
                char separator = ':';

                // Get the value for each header, strip out \r\n if found, then append it with the key.
                foreach (string headerValues in kvp.Value)
                {
                    string trimmedValue = headerValues.TrimStart().Replace("\r\n", string.Empty);
                    headerBuilder.Append(separator).Append(trimmedValue);

                    // Set this to a comma; this will only be used 
                    // if there are multiple values for one of the headers.
                    separator = ',';
                }
                sb.Append(headerBuilder.ToString()).Append("\n");
            }
            return sb.ToString();
        }

        private static string GetCanonicalizedResource(Uri address, string accountName)
        {
            // The absolute path is "/" because for we're getting a list of containers.
            StringBuilder sb = new StringBuilder("/").Append(accountName).Append(address.AbsolutePath);

            // Address.Query is the resource, such as "?api-version=".
            // This ends up with a NameValueCollection with 1 entry having key=comp, value=list.
            // It will have more entries if you have more query parameters.
            NameValueCollection values = HttpUtility.ParseQueryString(address.Query);

            foreach (var item in values.AllKeys.OrderBy(k => k))
            {
                sb.Append('\n').Append(item).Append(':').Append(values[item]);
            }

            return sb.ToString().ToLower();
        }
    }
}
