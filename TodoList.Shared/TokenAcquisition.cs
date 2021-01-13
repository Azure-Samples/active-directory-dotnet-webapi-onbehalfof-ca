using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.Net.Http.Headers;
using TodoList.Shared.TokenCacheProviders;

namespace TodoList.Shared
{
    public class TokenAcquisition
    {
        private IConfidentialClientApplication _application;

        private readonly MicrosoftIdentityOptions _microsoftIdentityOptions=new MicrosoftIdentityOptions();
        private readonly ConfidentialClientApplicationOptions _applicationOptions=new ConfidentialClientApplicationOptions();
        CacheType _cacheType;
        public TokenAcquisition(MicrosoftIdentityOptions microsoftIdentityOptions, ConfidentialClientApplicationOptions applicationOptions)

        {
            _microsoftIdentityOptions = microsoftIdentityOptions;
            _applicationOptions=applicationOptions;
        }
        public TokenAcquisition(MicrosoftIdentityOptions microsoftIdentityOptions, ConfidentialClientApplicationOptions applicationOptions, CacheType cacheType)

        {
            _microsoftIdentityOptions = microsoftIdentityOptions;
            _applicationOptions = applicationOptions;
            _cacheType = cacheType;
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
            string proposedAction = Constants.Consent;
            if (msalServiceException.ErrorCode == MsalError.InvalidGrantError && AcceptedTokenVersionMismatch(msalServiceException))
            {
                throw msalServiceException;
            }

            _application = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);
            try
            {

          
            string consentUrl = $"{_application.Authority}/oauth2/v2.0/authorize?client_id={_applicationOptions.ClientId}"
                + $"&response_type=code&redirect_uri={_application.AppConfig.RedirectUri}"
                + $"&response_mode=query&scope=offline_access%20{string.Join("%20", scopes)}";

            IDictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    { Constants.ConsentUrl, consentUrl },
                    { Constants.Claims, msalServiceException.Claims },
                    { Constants.Scopes, string.Join(",", scopes) },
                    { Constants.ProposedAction, proposedAction },
                };

            string parameterString = string.Join(", ", parameters.Select(p => $"{p.Key}=\"{p.Value}\""));


                if (httpResponse == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextAndHttpResponseAreNull);
                }

                var headers = httpResponse.Headers;
                httpResponse.StatusCode = (int)HttpStatusCode.Forbidden;

                headers[HeaderNames.WWWAuthenticate] = new StringValues($"{Constants.Bearer} {parameterString}");

            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }
        }

        public /* for testing */ async Task<IConfidentialClientApplication> GetOrBuildConfidentialClientApplicationAsync()
        {
            if (_application == null)
            {
                return await BuildConfidentialClientApplicationAsync().ConfigureAwait(false);
            }

            return _application;
        }

        /// <summary>
        /// Creates an MSAL confidential client application.
        /// </summary>
        public async Task<IConfidentialClientApplication> BuildConfidentialClientApplicationAsync()
        {
            var request = HttpContext.Current;
            var url = HttpContext.Current.Request.Url;
            string currentUri = null;

            if (!string.IsNullOrEmpty(_applicationOptions.RedirectUri))
            {
                currentUri = _applicationOptions.RedirectUri;
            }

            if (request != null && string.IsNullOrEmpty(currentUri))
            {
                currentUri = new UriBuilder(
                   url.Scheme,
                   url.Host,
                   url.Port).ToString();
            }

            PrepareAuthorityInstanceForMsal();

            if (!string.IsNullOrEmpty(_microsoftIdentityOptions.ClientSecret))
            {
                _applicationOptions.ClientSecret = _microsoftIdentityOptions.ClientSecret;
            }

            MicrosoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(
                 _applicationOptions.ClientSecret);

            try
            {
                var builder = ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(_applicationOptions);

                // The redirect URI is not needed for OBO
                if (!string.IsNullOrEmpty(currentUri))
                {
                    builder.WithRedirectUri(currentUri);
                }

                string authority;

                authority = $"{_applicationOptions.Instance}{_applicationOptions.TenantId}/";
                builder.WithAuthority(authority);


                IConfidentialClientApplication app = builder.Build();
                _application = app;

                // Initialize token cache providers
                // After the ConfidentialClientApplication is created, we overwrite its default UserTokenCache serialization with our implementation
                SetCache(app.UserTokenCache);

                return app;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Initialize token cache providers on the basis of input parameter i.e., tokenCache.
        /// </summary>
        /// <param name="tokenCache"></param>
        private void SetCache(ITokenCache tokenCache)
        {

            if (_cacheType == CacheType.InMemoryCache)
            {
                MSALPerUserMemoryCache mSALPerUserMemoryCache = new MSALPerUserMemoryCache(tokenCache);
            }
            else if (_cacheType == CacheType.FileCache)
            {
                TokenCacheHelper.EnableSerialization(tokenCache);
            }
        }

        /// <summary>
        /// For web APIs, acquire token on-behalf-of flow with the token used to call the API
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetUserTokenOnBehalfOfAsync(IEnumerable<string> requestedScopes)
        {
            string authority = $"{_applicationOptions.Instance}{_applicationOptions.TenantId}/";

            IConfidentialClientApplication app = BuildConfidentialClientApplicationAsync().Result;

            var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext;

            string userAccessToken = (string)bootstrapContext;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");
                var result = await app.AcquireTokenOnBehalfOf(requestedScopes, userAssertion)
                            .WithAuthority(authority)
                            .ExecuteAsync();
                return result;
            
        }

        /// <summary>
        /// For web app, gets an access token for a downstream API on behalf of the signed-in user..
        /// </summary>
        /// <param name="requestedScopes"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetAccessTokenForUserAsync(IEnumerable<string> requestedScopes)
        {
            try
            {
                IConfidentialClientApplication app = BuildConfidentialClientApplicationAsync().Result;

                AuthenticationResult result = null;
                IAccount account = await app.GetAccountAsync(ClaimsPrincipal.Current.GetAccountId());
                result = await app.AcquireTokenSilent(requestedScopes, account)
                   .ExecuteAsync();

                return result;
            }
            catch (MsalUiRequiredException ex)
            {
                // Case of the web app: we let the MsalUiRequiredException be caught by the
                // AuthorizeForScopesAttribute exception filter so that the user can consent, do 2FA, etc ...
                throw new MicrosoftIdentityWebChallengeUserException(ex, requestedScopes.ToArray(), null);
            }
        }
        private static bool AcceptedTokenVersionMismatch(MsalUiRequiredException msalServiceException)
        {
            // Normally app developers should not make decisions based on the internal AAD code
            // however until the STS sends sub-error codes for this error, this is the only
            // way to distinguish the case.
            // This is subject to change in the future
            return msalServiceException.Message.Contains(
                ErrorCodes.B2CPasswordResetErrorCode);
        }
        private void PrepareAuthorityInstanceForMsal()
        {
            if (_microsoftIdentityOptions.IsB2C && _applicationOptions.Instance.EndsWith("/tfp/"))
            {
                _applicationOptions.Instance = _applicationOptions.Instance.Replace("/tfp/", string.Empty).Trim();
            }

            _applicationOptions.Instance = _applicationOptions.Instance.TrimEnd('/') + "/";
        }

        /// <summary>
        /// Removes the account associated with ClaimsPrincipal.Current from the MSAL.NET cache.
        /// <returns>A <see cref="Task"/> that represents a completed account removal operation.</returns>
        public async Task RemoveAccountAsync()
        {
            IConfidentialClientApplication app = BuildConfidentialClientApplicationAsync().Result;

            // We only clear the user's tokens.
            SetCache(app.UserTokenCache);
            
            var userAccount = await app.GetAccountAsync(ClaimsPrincipal.Current.GetAccountId());
            if (userAccount != null)
            {
                await app.RemoveAsync(userAccount);
            }
        }
    }
}