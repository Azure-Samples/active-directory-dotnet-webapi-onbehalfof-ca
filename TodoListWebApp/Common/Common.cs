using Microsoft.Identity.Client;
using TodoList.Shared;
using System.Threading.Tasks;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Notifications;

namespace TodoListWebApp
{
    public static class Common
    {
        static TokenAcquisition _tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions(), CacheType.InMemoryCache);

        /// <summary>
        /// Creates an MSAL Confidential client application by calling BuildConfidentialClientApplicationAsync
        /// </summary>
        /// <returns></returns>
        public static IConfidentialClientApplication BuildConfidentialClientApplication()
        {
            var app = _tokenAcquisition.BuildConfidentialClientApplicationAsync().Result;
            return app;
        }

        /// <summary>
        /// Calls method to Removes the account from the MSAL.NET cache.
        /// </summary>
        /// <returns></returns>
        public static void RemoveAccount()
        {
            _tokenAcquisition.RemoveAccountAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Call method to acquire Access Token for the signed-in user.
        /// </summary>
        /// <returns></returns>
        public static async Task<AuthenticationResult> GetAccessTokenForUserAsync()
        {
             return await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { SetOptions.TodoListServiceScope });
        }

        /// <summary>
        /// Initializes OpenIdConnectAuthenticationOptions
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Handling the auth redemption by MSAL.NET so that a token is available in the token cache
        /// where it will be usable through the TokenAcquisition service
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            // Call MSAL.NET AcquireTokenByAuthorizationCode
            var application = BuildConfidentialClientApplication();
            var result = await application.AcquireTokenByAuthorizationCode(new[] { SetOptions.TodoListServiceScope },
                                                                     context.ProtocolMessage.Code)
                                    .ExecuteAsync();

            context.HandleCodeRedemption(null, result.IdToken);
        }
    }
}