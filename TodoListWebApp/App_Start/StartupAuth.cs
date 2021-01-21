using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Threading.Tasks;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            AddMicrosoftIdentityWebAppAuthentication(app);

            // This makes any middleware defined above this line run before the Authorization rule is applied in web.config
            app.UseStageMarker(PipelineStage.Authenticate);
        }

        /// <summary>
        /// Adds authentication for a web application with the Microsoft Identity platform
        /// </summary>
        /// <param name="app"></param>
        private void AddMicrosoftIdentityWebAppAuthentication(IAppBuilder app)
        {
            app.UseOpenIdConnectAuthentication(
                            new OpenIdConnectAuthenticationOptions
                            {
                                ClientId = AuthenticationConfig.ClientId,
                                ClientSecret = AuthenticationConfig.ClientSecret,
                                Authority = AuthenticationConfig.Authority,
                                RedirectUri = AuthenticationConfig.PostLogoutRedirectUri,
                                PostLogoutRedirectUri = AuthenticationConfig.PostLogoutRedirectUri,
                                ResponseType = "code",
                                Scope = $"{Constants.DefaultScopes} {AuthenticationConfig.TodoListServiceScope}",
                                Notifications = new OpenIdConnectAuthenticationNotifications()
                                {
                                    AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                                    AuthenticationFailed = OnAuthenticationFailed
                                }
                            });
        }

        /// <summary>
        /// Handle failed authentication requests by redirecting the user to the home page with an error in the query string
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task OnAuthenticationFailed(AuthenticationFailedNotification<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> arg)
        {
            arg.HandleResponse();
            arg.Response.Redirect("/?errormessage=" + arg.Exception.Message);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Handling the auth redemption by MSAL.NET for the web API so that a token is available in the token cache
        /// where it will be usable through the TokenAcquisition service
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            // Call MSAL.NET AcquireTokenByAuthorizationCode
            var application = Common.BuildConfidentialClientApplication();
            var result = await application.AcquireTokenByAuthorizationCode(new[] { AuthenticationConfig.TodoListServiceScope },
                                                                     context.ProtocolMessage.Code)
                                    .ExecuteAsync();

            context.HandleCodeRedemption(null, result.IdToken);
        }
    }
}