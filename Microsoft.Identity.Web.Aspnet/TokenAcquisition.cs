using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.Identity.Web.Aspnet
{
    public class TokenAcquisition
    {
        private IConfidentialClientApplication _application;

        public AuthenticationConfig AuthenticationConfig { get; }

        public TokenAcquisition(AuthenticationConfig authConfig)
        {
            this.AuthenticationConfig = authConfig;

            ConfidentialClientApplicationOptions confidentialClientOptions = new ConfidentialClientApplicationOptions()
            {
                ClientId = authConfig.ClientId,
                ClientSecret = authConfig.ClientSecret,
                EnablePiiLogging = true,
                Instance = authConfig.AADInstance,
                RedirectUri = authConfig.RedirectUri,
                TenantId = authConfig.RedirectUri
            };

            this._application = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(confidentialClientOptions).Build();
        }

        /// <summary>
        /// For web app, gets an access token for a downstream API on behalf of the signed-in user..
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetAccessTokenForUserAsync(IEnumerable<string> requestedScopes)
        {
            AuthenticationResult result = null;

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

            return result;

        }

        /// <summary>
        /// For web APIs, acquire token on-behalf-of flow with the token used to call the API
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetUserTokenOnBehalfOfAsync(IEnumerable<string> requestedScopes)
        {
            string authority = $"{AuthenticationConfig.AADInstance}{AuthenticationConfig.TenantId}/";

            //IConfidentialClientApplication app = await BuildConfidentialClientApplicationAsync();

            var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext;

            string userAccessToken = (string)bootstrapContext;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");
            var result = await _application.AcquireTokenOnBehalfOf(requestedScopes, userAssertion)
                        .WithAuthority(authority)
                        .ExecuteAsync();
            return result;

        }
    }
}