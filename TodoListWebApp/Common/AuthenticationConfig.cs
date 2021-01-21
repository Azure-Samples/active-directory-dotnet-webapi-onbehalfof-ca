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

        public static string Authority = String.Format(CultureInfo.InvariantCulture, AADInstance, TenantId);
      
        public static string PostLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        //public static string Instance = ConfigurationManager.AppSettings["ida:Instance"];

        public static string TodoListServiceScope = ConfigurationManager.AppSettings["ida:TodoListServiceScope"];
    }
}