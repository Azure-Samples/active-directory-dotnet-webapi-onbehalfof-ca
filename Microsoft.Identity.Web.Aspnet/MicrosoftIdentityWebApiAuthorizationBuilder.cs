using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;

namespace Microsoft.Identity.Web.Aspnet
{
    public static class MicrosoftIdentityWebApiAuthorizationBuilder
    {
        /// <summary>
        /// Protects the web API with the Microsoft Identity platform
        /// </summary>
        /// <param name="app"></param>
        /// <param name="webApiConfig">A set of config to configure protection aspects</param>
        public static void ProtectWebApiWithMicrosoftIdentity(this IAppBuilder app, JwtBearerOptions webApiConfig)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    Tenant = webApiConfig.TenantId,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        SaveSigninToken = true,
                        ValidAudiences = webApiConfig.ValidAudiences
                    }
                });
        }
    }
}