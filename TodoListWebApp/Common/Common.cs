using Microsoft.Identity.Client;
using TodoList.Shared;
using System;
using System.Security.Claims;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Notifications;

namespace TodoListWebApp
{
    public static class Common
    {
        static TokenAcquisition _tokenAcquisition = null;

        /// <summary>
        /// Creates an MSAL Confidential client application
        /// </summary>
        /// <param name="httpContext">HttpContext associated with the OIDC response</param>
        /// <param name="claimsPrincipal">Identity for the signed-in user</param>
        /// <returns></returns>
        public static IConfidentialClientApplication BuildConfidentialClientApplication()
        {
            _tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions(), CacheType.InMemoryCache);
            var app = _tokenAcquisition.BuildConfidentialClientApplicationAsync().Result;
            return app;
        }
        public static void RemoveAccount()
        {
            _tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions(), CacheType.InMemoryCache);
            _tokenAcquisition.RemoveAccount().ConfigureAwait(false);
        }
        public static async Task<AuthenticationResult> GetAccessTokenForUserAsync()
        {
            var app = BuildConfidentialClientApplication();
            AuthenticationResult result = null;
            IAccount account = await app.GetAccountAsync(ClaimsPrincipal.Current.GetAccountId());
            try
            {
                result = await app.AcquireTokenSilent(new[] { SetOptions.TodoListServiceScope }, account)
                   .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                IncrementalConsentExceptionHandler(ex);
            }
            return result;
        }
        public static void IncrementalConsentExceptionHandler(MsalUiRequiredException ex)
        {
            string redirectUri = HttpContext.Current.Request.Url.ToString();

            if (CanBeSolvedByReSignInOfUser(ex))
            {
                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                   new AuthenticationProperties { RedirectUri = redirectUri },
                   OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }
        private static bool CanBeSolvedByReSignInOfUser(MsalUiRequiredException ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            // ex.ErrorCode != MsalUiRequiredException.UserNullError indicates a cache problem.
            return ex.ErrorCode.ContainsAny(new[] { MsalError.UserNullError, MsalError.InvalidGrantError });
        }
        public static OpenIdConnectAuthenticationOptions GetOpenIdConnectAuthenticationOptions()
        {
            return new OpenIdConnectAuthenticationOptions
            {
                ClientId = AuthenticationConfig.ClientId,
                ClientSecret = AuthenticationConfig.ClientSecret,
                Authority = AuthenticationConfig.Authority,
                RedirectUri = AuthenticationConfig.PostLogoutRedirectUri,
                PostLogoutRedirectUri = AuthenticationConfig.PostLogoutRedirectUri,
                ResponseType = "code",
                Scope = "openid profile offline_access " + SetOptions.TodoListServiceScope,
                Notifications = new OpenIdConnectAuthenticationNotifications()
                {
                    AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                    AuthenticationFailed = (context) =>
                    {
                        return Task.FromResult(0);
                    }
                }
            };
        }
        private static async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            // Call MSAL.NET AcquireTokenByAuthorizationCode
            var application = Common.BuildConfidentialClientApplication();
            var result = await application.AcquireTokenByAuthorizationCode(new[] { SetOptions.TodoListServiceScope },
                                                                     context.ProtocolMessage.Code)
                                    .ExecuteAsync();

            context.HandleCodeRedemption(null, result.IdToken);
        }
    }
}