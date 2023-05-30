using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Org.BouncyCastle.OpenSsl;
using SasTokenGeneration.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace SasTokenGeneration.Services
{
    public class GithubService : IGithubService
    {
        private readonly string _installationId;
        private readonly string _portalRepositoryId;
        private readonly string _apiRepositoryId;
        private readonly string _publicKey;
        private readonly string _publicKeyId;
        private readonly string _environmentName;
        private readonly string _secretName;
        private readonly string _baseUrl;
        private readonly string _apimId;
        private readonly string _apimKey;
        private readonly string _apimKey_Test;
        private readonly string _apimKey_Prod;
        private readonly string _environmentName_Scheduled;
        private readonly string _scheduled_Dev_Token;
        private readonly string _scheduled_Test_Token;
        private readonly string _scheduled_Prod_Token;
        private string _encodeToken;
        private string _apiToken;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GithubService> _log;
        public GithubService(IHttpClientFactory httpClientFactory, IOptions<GithubApiSettings> githubApiSettings, ILogger<GithubService> log, IOptions<AzApimSettings> azApimSettings)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _installationId = githubApiSettings.Value.InstallationId;
            _portalRepositoryId = githubApiSettings.Value.PortalRepositoryId;
            _apiRepositoryId = githubApiSettings.Value.ApiRepositoryId;
            _publicKey = githubApiSettings.Value.PublicKey;
            _secretName = githubApiSettings.Value.SecretName;
            _environmentName = githubApiSettings.Value.EnvironmentName;
            _baseUrl = githubApiSettings.Value.BaseUrl;
            _publicKeyId = githubApiSettings.Value.PublicKeyId;
            _environmentName_Scheduled = githubApiSettings.Value.EnvironmentName_Scheduled;
            _scheduled_Dev_Token = githubApiSettings.Value.Scheduled_Dev_Token;
            _scheduled_Test_Token = githubApiSettings.Value.Scheduled_Test_Token;
            _scheduled_Prod_Token = githubApiSettings.Value.Sheduled_Prod_Token;
            _apimKey = azApimSettings.Value.ApimKey;
            _apimId = azApimSettings.Value.ApimId;
            _apimKey_Test = azApimSettings.Value.ApimKey_Test;
            _apimKey_Prod = azApimSettings.Value.ApimKey_Prod;
            _log = log;
        }

        public string GenerateSasToken()
        {
            string apimkey = string.Empty;
            if (_environmentName == "Development")
            {
                apimkey = _apimKey;
            }
            else if (_environmentName == "Test")
            {
                apimkey = _apimKey_Test;
            }
            else if (_environmentName == "Production")
            {
                apimkey = _apimKey_Prod;
            }

            using (var encoder = new HMACSHA512(Encoding.UTF8.GetBytes(apimkey)))
            {
                var d = DateTime.UtcNow.AddDays(30);
                var expiry = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, DateTimeKind.Utc);

                var dataToSign = _apimId + "\n" + expiry.ToString("O", CultureInfo.InvariantCulture);
                var hash = encoder.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
                var signature = Convert.ToBase64String(hash);
                var encodedToken = $"SharedAccessSignature {_apimId}&{expiry:yyyyMMddHHmmss}&{signature}";

                string base64string = EncodeSodium(encodedToken);
                _apiToken = EncodeSodium(encodedToken.Remove(0, 22));
                return base64string;
            }
        }
        public string EncodeSodium(string encodedToken)
        {
            var secretValue = Encoding.UTF8.GetBytes(encodedToken);
            var publicKey = Convert.FromBase64String(_publicKey);
            var sealedPublicKeyBox = Sodium.SealedPublicKeyBox.Create(secretValue, publicKey);


            _log?.LogInformation($"SealedPublicKeyBox: {Convert.ToBase64String(sealedPublicKeyBox)}");
            return Convert.ToBase64String(sealedPublicKeyBox);
        }
        public async Task<string> generateTokenAsync(ExecutionContext context)
        {
            try
            {
                _log?.LogInformation($"GenerateTokenAsync Entered");
                string privateFile = Path.Combine(context.FunctionAppDirectory, "privatekey.pem");//File.ReadAllText(@"C:\Manimozhi\privatekey.pem");
                string privateKey = File.ReadAllText(privateFile);
                RSAParameters rsaParams;
                var responseBody = string.Empty;
                var jwtToken = string.Empty;
                string secretPortalUrl = "/repositories/" + _portalRepositoryId + "/environments/" + _environmentName + "/secrets/" + _secretName;
                string secretApiUrl = "/repositories/" + _apiRepositoryId + "/environments/" + _environmentName + "/secrets/" + _secretName;
                string requestUrl = "/app/installations/" + _installationId + "/access_tokens";
                var payload = new Dictionary<string, object>
                {
                    { "iss", "299687"},
                    { "iat", DateTimeOffset.Now.ToUnixTimeSeconds()},
                    { "exp", DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds()}
                };

                using (var tr = new StringReader(privateKey))
                {
                    var pemReader = new PemReader(tr);
                    var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                    if (keyPair == null)
                    {
                        throw new Exception("Could not read RSA private key");
                    }
                    var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
                    rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
                }

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(rsaParams);
                    responseBody = await SendRequest(requestUrl, Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256), new HttpContentBody { });

                    var token = JsonConvert.DeserializeObject<AccessToken>(responseBody);

                    if (token != null)
                    {
                        jwtToken = token.token.ToString();
                    }
                }
                var secretKey = GenerateSasToken();
                var secretContent = new HttpContentBody
                {
                    encrypted_value = secretKey,
                    key_id = _publicKeyId
                };
                var secretPortalResponse = await SendRequest(secretPortalUrl, jwtToken, secretContent);

                if (secretPortalResponse != null)
                {
                    //Scheduled Env request
                    string scheduledPortalUrl = "/repositories/" + _portalRepositoryId + "/environments/" + _environmentName_Scheduled + "/secrets/";
                    if (_environmentName == "Development")
                    {
                        scheduledPortalUrl = scheduledPortalUrl + _scheduled_Dev_Token;
                    }
                    else if (_environmentName == "Test")
                    {
                        scheduledPortalUrl = scheduledPortalUrl + _scheduled_Test_Token;
                    }
                    else if (_environmentName == "Production")
                    {
                        scheduledPortalUrl = scheduledPortalUrl + _scheduled_Prod_Token;
                    }
                    var scheduledResponse = await SendRequest(scheduledPortalUrl, jwtToken, secretContent);

                    //Unified Portal request
                    var apiContent = new HttpContentBody
                    {
                        encrypted_value = _apiToken,
                        key_id = _publicKeyId
                    };
                    await SendRequest(secretApiUrl, jwtToken, apiContent);
                }

                return secretPortalResponse;
            }
            catch (Exception ex) { _log?.LogError("Exception ", ex.Message); }
            return null;
        }
        public Task<string> SendRequest(string requestUrl, string tokenDetails, HttpContentBody content)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var tokenResponse = new HttpResponseMessage();
                httpClient.BaseAddress = new Uri(_baseUrl);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                httpClient.DefaultRequestHeaders.Add("User-Agent", "SasKeyApp");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenDetails);

                if (content.encrypted_value != null)
                {
                    var jsonContent = JsonConvert.SerializeObject(content);
                    var Scontent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    tokenResponse = httpClient.PutAsync(requestUrl, Scontent).Result;
                    return Task.FromResult(tokenResponse.StatusCode.ToString());
                }
                else
                    tokenResponse = httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(new Dictionary<string, string> { })).Result;

                if (tokenResponse.IsSuccessStatusCode)
                {
                    return Task.FromResult(tokenResponse.Content.ReadAsStringAsync().Result);
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
