using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Web.Aspnet
{
    public class CommonUtil
    {
        public static string EnsureAuthorityIsV2(string authority)
        {
            authority = authority.Trim().TrimEnd('/');
            if (!authority.EndsWith("v2.0", StringComparison.InvariantCulture))
            {
                authority += "/v2.0";
            }

            return authority;
        }

        public static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
        /// <summary>
        /// Create Response Content for insufficient claims.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="addionalInfo"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        internal static InsufficientClaimsResponse CreateErrorResponseMessage(string message, string addionalInfo = null, string requestId = null)
        {
            InsufficientClaimsResponse insufficientClaimsResponse = new InsufficientClaimsResponse();
            insufficientClaimsResponse.Code = Constants.InsufficientClaims;
            insufficientClaimsResponse.Message = message;
            insufficientClaimsResponse.AdditionalInfo = addionalInfo;

            insufficientClaimsResponse.InnerError = new ResponseInnerError();

            insufficientClaimsResponse.InnerError.Date = DateTime.UtcNow;

            if (requestId == null)
            {
                requestId = Guid.NewGuid().ToString();
            }
            insufficientClaimsResponse.InnerError.RequestId = requestId;
            insufficientClaimsResponse.InnerError.ClientRequestId = requestId;
            return insufficientClaimsResponse;
        }

        /// <summary>
        /// Creates header value for insufficient claims. 
        /// </summary>
        /// <param name="authenticationConfig"></param>
        /// <param name="claims"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        internal static string CreateResponseHeader(AuthenticationConfig authenticationConfig, string claims, IEnumerable<string> scopes=null)
        {
            string authorization_uri = $"{authenticationConfig.Authority}/oauth2/v2.0/authorize";

            IDictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    {Constants.Realm,"" },
                    { Constants.AuthorizationUri, authorization_uri },
                    { Constants.Claims, claims },
                    { Constants.Scopes, string.Join(",", scopes!=null? string.Join("%20", scopes):"") },
                    { Constants.Error, Constants.InsufficientClaims },
                };

            string parameterString = string.Join("; ", parameters.Select(p => $"{p.Key}=\"{p.Value}\""));
            return parameterString;
        }
    }
}