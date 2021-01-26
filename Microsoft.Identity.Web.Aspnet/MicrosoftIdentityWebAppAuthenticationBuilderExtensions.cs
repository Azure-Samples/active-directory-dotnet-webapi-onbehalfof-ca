using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    /// <summary>
    /// Contains the initialization methods to integrated with the Microsoft Identity Platform
    /// </summary>
    public static class MicrosoftIdentityWebAppAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Configures the web application to just sign-in a user and get an id_token
        /// </summary>
        /// <param name="app"></param>
        /// <param name="authenticationConfig"></param>
        public static void AddMicrosoftIdentityWebAppAuthentication(this IAppBuilder app, AuthenticationConfig authenticationConfig)
        {
            app.UseOpenIdConnectAuthentication(
                    new OpenIdConnectAuthenticationOptions
                    {
                        ClientId = authenticationConfig.ClientId,
                        Authority = authenticationConfig.Authority,
                        PostLogoutRedirectUri = authenticationConfig.PostLogoutRedirectUri,
                        RedirectUri = authenticationConfig.RedirectUri,
                        Scope = IdentityConstants.DefaultScopes,
                        ResponseType = "id_token",
                        TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false, NameClaimType = IdentityConstants.PreferredUserName },
                    });
        }

        public static void EnableTokenAcquisitionToCallDownstreamApi(this IAppBuilder app, AuthenticationConfig authenticationConfig, IEnumerable<string> initialScopes = null, CacheType cacheType = CacheType.InMemoryCache)
        {
            ApplicationBuilders applicationBuilders = new ApplicationBuilders(cacheType);

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = authenticationConfig.ClientId,
                    Authority = authenticationConfig.Authority,
                    PostLogoutRedirectUri = authenticationConfig.PostLogoutRedirectUri,
                    RedirectUri = authenticationConfig.RedirectUri,
                    TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false, NameClaimType = IdentityConstants.PreferredUserName },
                    ResponseType = "code",
                    Scope = $"{IdentityConstants.DefaultScopes} {string.Join(" ", initialScopes)}",
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthorizationCodeReceived = async context =>
                        {
                            // Call MSAL.NET AcquireTokenByAuthorizationCode
                            var application = applicationBuilders.BuildConfidentialClientApplicationAsync(authenticationConfig);
                            var result = await application.AcquireTokenByAuthorizationCode(initialScopes, context.ProtocolMessage.Code)
                                                    .ExecuteAsync();

                            context.HandleCodeRedemption(null, result.IdToken);
                        },
                        AuthenticationFailed = arg =>
                        {
                            arg.HandleResponse();
                            arg.Response.Redirect("/?errormessage=" + arg.Exception.Message);

                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}