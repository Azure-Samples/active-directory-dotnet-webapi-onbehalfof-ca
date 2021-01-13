using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace TodoListService
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


        /// <summary>
        /// Tenant Id of the tenant where the application is registered.
        /// </summary>
        public static string TenantId = ConfigurationManager.AppSettings["ida:TenantId"];

    }
}