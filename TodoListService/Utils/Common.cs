using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using TodoList.Shared;

namespace TodoListService.Utils
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
            _tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions());
            var app = _tokenAcquisition.BuildConfidentialClientApplicationAsync().Result;
            return app;
        }
        public static void RemoveAccount()
        {
            _tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions());
            _tokenAcquisition.RemoveAccount().ConfigureAwait(false);
        }
    }
    public class SetOptions
    {
        public static string instance = ConfigurationManager.AppSettings["ida:Instance"];
        public static string TodoListScope = ConfigurationManager.AppSettings["ida:TodoListScope"];
        private static MicrosoftIdentityOptions IdentityOptions = new MicrosoftIdentityOptions();
        private static ConfidentialClientApplicationOptions ApplicationOptions = new ConfidentialClientApplicationOptions();

        public static ConfidentialClientApplicationOptions SetConClientAppOptions()
        {
            ApplicationOptions.Instance = instance;
            ApplicationOptions.TenantId = AuthenticationConfig.TenantId;
            ApplicationOptions.RedirectUri = AuthenticationConfig.RedirectUri;
            ApplicationOptions.ClientId = AuthenticationConfig.ClientId;
            return ApplicationOptions;
        }
        public static MicrosoftIdentityOptions SetMicrosoftIdOptions()
        {
            IdentityOptions.ClientId = AuthenticationConfig.ClientId;
            IdentityOptions.ClientSecret = AuthenticationConfig.ClientSecret;
            IdentityOptions.RedirectUri = AuthenticationConfig.RedirectUri;
            return IdentityOptions;
        }
    }
}