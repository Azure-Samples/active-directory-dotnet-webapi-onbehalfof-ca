using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    internal class ExtractAuthenticationHeader
    {
        /// <summary>
        /// Extract claims and scopes from WwwAuthenticate header and returns key value pair.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        internal static Dictionary<string, string> ExtractHeaderValues(HttpResponseMessage response)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            var header = response.Headers.WwwAuthenticate.ToString();
            if (!string.IsNullOrEmpty(header))
            {
                if (header.StartsWith("Bearer", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    header = header.Remove(0, "Bearer".Length);
                }

                IEnumerable<string> parameters = header.Split(';').Select(v => v.Trim()).ToList();

                string claims = GetParameter(parameters, Constants.Claims);

                string scopes = GetParameter(parameters, Constants.Scopes);

                string error = GetParameter(parameters, Constants.Error);

                if (!string.IsNullOrEmpty(claims))
                {
                    keyValues.Add(Constants.Claims, ConvertBase64String(claims));
                }
                if (!string.IsNullOrEmpty(scopes))
                {
                    keyValues.Add(Constants.Scopes, scopes);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    keyValues.Add(Constants.Error, error);
                }
            }
            return keyValues;
        }

        private static string GetParameter(IEnumerable<string> parameters, string parameterName)
        {
            int offset = parameterName.Length + 1;
            return parameters.FirstOrDefault(p => p.StartsWith($"{parameterName}="))?.Substring(offset)?.Trim('"');
        }

        /// <summary>
        /// Checks and if input is base-64 encoded string then decodes it.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static string ConvertBase64String(string inputString)
        {
            if (inputString == null || inputString.Length == 0 || inputString.Length % 4 != 0 || inputString.Contains(" ") || inputString.Contains("\t") || inputString.Contains("\r") || inputString.Contains("\n"))
            {
                return inputString;
            }

            try
            {
                var claimChallengebase64Bytes = Convert.FromBase64String(inputString);
                var claimChallenge = System.Text.Encoding.UTF8.GetString(claimChallengebase64Bytes);
                return claimChallenge;
            }
            catch (Exception)
            {
                return inputString;
            }
        }
    }
}
