using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace UTwitter
{
    public class TwitterRestClient
    {
        private static readonly string[] OAuthParametersToIncludeInHeader = new[]
                                                            {
                                                                "oauth_version",
                                                                "oauth_nonce",
                                                                "oauth_timestamp",
                                                                "oauth_signature_method",
                                                                "oauth_consumer_key",
                                                                "oauth_token",
                                                                "oauth_verifier"
                                                                // Leave signature omitted from the list, it is added manually
                                                                // "oauth_signature",
                                                            };

        private static readonly string[] SecretParameters = new[]
                                                                {
                                                                    "oauth_consumer_secret",
                                                                    "oauth_token_secret",
                                                                    "oauth_signature"
                                                                };

        private string _authToken;
        private string _authSecret;
        private string _consumerKey;
        private string _consumerSecret;
        private MonoBehaviour _monobehaviour;
        public TwitterRestClient(MonoBehaviour monobehaviour, string consumerKey, string consumerSecret)
        {
            _monobehaviour = monobehaviour;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
        }

        public bool Authenticated
        {
            get
            {
                return !string.IsNullOrEmpty(_authToken) && !string.IsNullOrEmpty(_authSecret);
            }
        }

        public void SetAuthToken(string authToken, string authSecret)
        {
            _authToken = authToken;
            _authSecret = authSecret;
        }

        private IEnumerator post(UnityWebRequest request, System.Action<Exception, string> callback)
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError)
            {
                Debug.LogWarning(string.Format("failed. {0}\n{1}", request.error, request.downloadHandler.text));
                callback(new Exception(string.Format("{0}:{1}", request.error, request.downloadHandler.text)), "");
            }
            else
            {
                if (request.responseCode >= 200 && request.responseCode < 300)
                {
                    Debug.Log(request.responseCode);
                    callback(null, request.downloadHandler.text);
                }
                else
                {
                    Debug.LogWarning(string.Format("failed. {0} {1} {2}", request.responseCode, request.error, request.downloadHandler.text));
                    callback(new Exception(string.Format("{0}:{1}", request.error, request.downloadHandler.text)), "");
                }
            }
        }

        public void Post(UnityWebRequest request, System.Action<Exception, string> callback)
        {
            _monobehaviour.StartCoroutine(post(request, callback));
        }

        public string GetHeader(string httpRequestType, string apiURL, Dictionary<string, string> parameters)
        {
            return GetHeaderWithAccessToken(httpRequestType, apiURL, _consumerKey, _consumerSecret, _authToken, _authSecret, parameters);
        }

        private static string GetHeaderWithAccessToken(string httpRequestType, string apiURL, string consumerKey, string consumerSecret, string token, string tokenSecret, Dictionary<string, string> parameters)
        {
            AddDefaultOAuthParams(parameters, consumerKey, consumerSecret);

            parameters.Add("oauth_token", token);
            parameters.Add("oauth_token_secret", tokenSecret);

            return GetFinalOAuthHeader(httpRequestType, apiURL, parameters);
        }

        private static void AddDefaultOAuthParams(Dictionary<string, string> parameters, string consumerKey, string consumerSecret)
        {
            parameters.Add("oauth_version", "1.0");
            parameters.Add("oauth_nonce", GenerateNonce());
            parameters.Add("oauth_timestamp", GenerateTimeStamp());
            parameters.Add("oauth_signature_method", "HMAC-SHA1");
            parameters.Add("oauth_consumer_key", consumerKey);
            parameters.Add("oauth_consumer_secret", consumerSecret);
        }

        private static string GetFinalOAuthHeader(string HTTPRequestType, string URL, Dictionary<string, string> parameters)
        {
            // Add the signature to the oauth parameters
            string signature = GenerateSignature(HTTPRequestType, URL, parameters);

            parameters.Add("oauth_signature", signature);

            StringBuilder authHeaderBuilder = new StringBuilder();
            authHeaderBuilder.AppendFormat("OAuth realm=\"{0}\"", "Twitter API");

            SortedDictionary<string, string> sortedParameters = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                if (Array.IndexOf(OAuthParametersToIncludeInHeader, parameter.Key) >= 0)
                {
                    sortedParameters.Add(parameter.Key, UrlEncode(parameter.Value));
                }
            }

            foreach (var item in sortedParameters)
            {
                authHeaderBuilder.AppendFormat(",{0}=\"{1}\"", UrlEncode(item.Key), UrlEncode(item.Value));
            }

            authHeaderBuilder.AppendFormat(",oauth_signature=\"{0}\"", UrlEncode(parameters["oauth_signature"]));

            return authHeaderBuilder.ToString();
        }

        private static string GenerateSignature(string httpMethod, string url, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> nonSecretParameters = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                bool found = false;
                foreach (string secretParameter in SecretParameters)
                {
                    if (secretParameter == parameter.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    nonSecretParameters.Add(parameter.Key, parameter.Value);
            }

            // Create the base string. This is the string that will be hashed for the signature.
            string signatureBaseString = string.Format(CultureInfo.InvariantCulture,
                                                        "{0}&{1}&{2}",
                                                        httpMethod,
                                                        UrlEncode(NormalizeUrl(new Uri(url))),
                                                        UrlEncode(nonSecretParameters));

            // Create our hash key (you might say this is a password)
            string key = string.Format(CultureInfo.InvariantCulture,
                                        "{0}&{1}",
                                        UrlEncode(parameters["oauth_consumer_secret"]),
                                        parameters.ContainsKey("oauth_token_secret") ? UrlEncode(parameters["oauth_token_secret"]) : string.Empty);


            // Generate the hash
            HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.ASCII.GetBytes(key));
            byte[] signatureBytes = hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
            return Convert.ToBase64String(signatureBytes);
        }

        private static string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
        }

        private static string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return new System.Random().Next(123400, int.MaxValue).ToString("X", CultureInfo.InvariantCulture);
        }

        private static string NormalizeUrl(Uri url)
        {
            string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", url.Scheme, url.Host);
            if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443)))
            {
                normalizedUrl += ":" + url.Port;
            }

            normalizedUrl += url.AbsolutePath;
            return normalizedUrl;
        }

        private static string UrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = Uri.EscapeDataString(value);

            // UrlEncode escapes with lowercase characters (e.g. %2f) but oAuth needs %2F
            value = Regex.Replace(value, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());

            // these characters are not escaped by UrlEncode() but needed to be escaped
            value = value
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27");

            // these characters are escaped by UrlEncode() but will fail if unescaped!
            value = value.Replace("%7E", "~");

            return value;
        }

        private static string UrlEncode(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            StringBuilder parameterString = new StringBuilder();

            SortedDictionary<string, string> paramsSorted = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                paramsSorted.Add(parameter.Key, parameter.Value);
            }

            foreach (var item in paramsSorted)
            {
                if (parameterString.Length > 0)
                {
                    parameterString.Append("&");
                }

                parameterString.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1}",
                        UrlEncode(item.Key),
                        UrlEncode(item.Value)));
            }

            return UrlEncode(parameterString.ToString());
        }
    }
}
