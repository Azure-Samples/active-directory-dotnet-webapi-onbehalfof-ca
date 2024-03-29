﻿using Microsoft.Identity.Web.Aspnet;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;

namespace MyWebApplication
{
    public partial class Startup
    {
        //private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        //private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        //private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        //private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        //public static string redirectUri = "https://localhost:44397/";
        //private static string authority = aadInstance + tenantId;

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            //app.UseOpenIdConnectAuthentication(
            //    new OpenIdConnectAuthenticationOptions
            //    {
            //        ClientId = clientId,
            //        Authority = authority,
            //        PostLogoutRedirectUri = postLogoutRedirectUri
            //    });

            app.AddMicrosoftIdentityWebAppAuthentication(new AuthenticationConfig());
        }

        private static string EnsureTrailingSlash(string value)
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
    }
}