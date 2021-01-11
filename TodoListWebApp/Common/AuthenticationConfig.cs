using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace TodoListWebApp
{
    public static class AuthenticationConfig
    {
        //public const string IssuerClaim = "iss";
        //public const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        //public const string MicrosoftGraphGroupsApi = "https://graph.microsoft.com/v1.0/groups";
        //public const string MicrosoftGraphUsersApi = "https://graph.microsoft.com/v1.0/users";
        //public const string AdminConsentFormat = "https://login.microsoftonline.com/{0}/adminconsent?client_id={1}&state={2}&redirect_uri={3}";
        //public const string BasicSignInScopes = "openid profile offline_access";
        //public const string NameClaimType = "name";

        /// <summary>
        /// The Client ID is used by the application to uniquely identify itself to Azure AD.
        /// </summary>
        public static string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// The ClientSecret is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        /// </summary>
        public static string ClientSecret { get; } = ConfigurationManager.AppSettings["ida:ClientSecret"];

        /// <summary>
        /// The Redirect Uri is the URL where the user will be redirected after they sign in.
        /// </summary>
        public static string RedirectUri { get; } = ConfigurationManager.AppSettings["ida:RedirectUri"];

        public static string AADInstance { get; } = ConfigurationManager.AppSettings["ida:AADInstance"];

        public static string TenantId = ConfigurationManager.AppSettings["ida:TenantId"];

        public static string Authority = String.Format(CultureInfo.InvariantCulture, AADInstance, AuthenticationConfig.TenantId);
      
        public static string PostLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
    }
}