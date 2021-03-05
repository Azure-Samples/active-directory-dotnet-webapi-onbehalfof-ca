using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Identity.Web.Aspnet
{
    public class TokenAcquisition
    {
        private IConfidentialClientApplication _application;

        private CacheType CacheType = CacheType.InMemoryCache;

        public AuthenticationConfig AuthenticationConfig { get; }

        public TokenAcquisition(AuthenticationConfig authConfig, CacheType cacheType = CacheType.InMemoryCache)
        {
            this.AuthenticationConfig = authConfig;
        }

        public void PrepareConfidentialClientInstanceAsync()
        {
            if (this._application == null)
            {
                var appBuilder = new ApplicationBuilders(this.CacheType);
                //ConfidentialClientApplicationOptions confidentialClientOptions = new ConfidentialClientApplicationOptions()
                //{
                //    ClientId = authConfig.ClientId,
                //    ClientSecret = authConfig.ClientSecret,
                //    EnablePiiLogging = true,
                //    Instance = authConfig.AADInstance,
                //    RedirectUri = authConfig.RedirectUri,
                //    TenantId = authConfig.RedirectUri
                //};

                // this._application = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(confidentialClientOptions).Build();
                this._application = appBuilder.BuildConfidentialClientApplication(this.AuthenticationConfig);
            }
        }

        /// <summary>
        /// For web app, gets an access token for a downstream API on behalf of the signed-in user..
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetAccessTokenForUserAsync(IEnumerable<string> requestedScopes)
        {
            AuthenticationResult result = null;
            this.PrepareConfidentialClientInstanceAsync();

            try
            {
                IAccount account = await _application.GetAccountAsync(ClaimsPrincipal.Current.GetMsalAccountId());
                result = await _application.AcquireTokenSilent(requestedScopes, account)
                   .ExecuteAsync();

                return result;
            }
            catch (MsalUiRequiredException ex)
            {
                // Case of the web app: we let the MsalUiRequiredException be caught by the
                // AuthorizeForScopesAttribute exception filter so that the user can consent, do 2FA, etc ...
                //throw new MicrosoftIdentityWebChallengeUserException(ex, requestedScopes.ToArray(), null);

                throw ex;
            }

        }

        /// <summary>
        /// For web APIs, acquire token on-behalf-of flow with the token used to call the API
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetUserTokenOnBehalfOfAsync(IEnumerable<string> requestedScopes)
        {
            string authority = $"{AuthenticationConfig.AADInstance}{AuthenticationConfig.TenantId}/";
            this.PrepareConfidentialClientInstanceAsync();

            //IConfidentialClientApplication app = await BuildConfidentialClientApplicationAsync();

            var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext;

            string userAccessToken = (string)bootstrapContext;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");
            var result = await _application.AcquireTokenOnBehalfOf(requestedScopes, userAssertion)
                        .WithAuthority(authority)
                        .ExecuteAsync();
            return result;
        }

        /// <summary>
        /// Used in web APIs (no user interaction).
        /// Replies to the client through the HTTP response by sending a 403 (forbidden) and populating the 'WWW-Authenticate' header so that
        /// the client, in turn, can trigger a user interaction so that the user consents to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException">The <see cref="MsalUiRequiredException"/> that triggered the challenge.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse httpResponse = null)
        {
            // A user interaction is required, but we are in a web API, and therefore, we need to report back to the client through a 'WWW-Authenticate' header https://tools.ietf.org/html/rfc6750#section-3.1

            try
            {
                IDictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    { Constants.Claims, msalServiceException.Claims },
                    { Constants.Scopes, string.Join(",", scopes) },
                    { Constants.ProposedAction, "" },
                };

                string parameterString = string.Join("; ", parameters.Select(p => $"{p.Key}=\"{p.Value}\""));

                var headers = httpResponse.Headers;
                httpResponse.StatusCode = (int)HttpStatusCode.Forbidden;

                headers[HeaderNames.WWWAuthenticate] = new StringValues($"Bearer {parameterString}");

                httpResponse.Write("insufficient_claims");
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }
        }
    }
}