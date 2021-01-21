using Microsoft.Identity.Client;
using System.Configuration;
using TodoList.Shared;

namespace TodoListWebApp
{
    public class SetOptions
    {
       
        private static MicrosoftIdentityOptions IdentityOptions = new MicrosoftIdentityOptions();
        private static ConfidentialClientApplicationOptions ApplicationOptions = new ConfidentialClientApplicationOptions();

        public static ConfidentialClientApplicationOptions SetConClientAppOptions()
        {
            ApplicationOptions.Instance = AuthenticationConfig.AADInstance;
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