using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Aspnet
{
    /// <summary>
    /// Contains the initialization methods to integrated with the Microsoft Identity Platform
    /// </summary>
    public static class MicrosoftIdentityWebAppAuthenticationBuilderExtensions
    {
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
    }
}
