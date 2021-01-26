using System.Configuration;

namespace Microsoft.Identity.Web.Aspnet
{
    /// <summary>
    /// A set of config items related to Identity configuration
    /// Auto-initializes from a set of predefined config keys
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// The Client ID is used by the application to uniquely identify itself to Azure AD.
        /// </summary>
        public string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// The ClientSecret is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        /// </summary>
        public string ClientSecret { get; } = ConfigurationManager.AppSettings["ida:ClientSecret"];

        /// <summary>
        /// The Redirect Uri is the URL where the user will be redirected after they sign in.
        /// </summary>
        public string RedirectUri { get; } = ConfigurationManager.AppSettings["ida:RedirectUri"];

        /// <summary>
        /// The instance of Azure Ad the user is trying to sign-in to
        /// </summary>
        public string AADInstance { get; } = ConfigurationManager.AppSettings["ida:Instance"];

        /// <summary>
        /// The Id of the Azure AD tenant
        /// </summary>
        public string TenantId { get; } = ConfigurationManager.AppSettings["ida:TenantId"];

        /// <summary>
        /// The domain of the Azure AD tenant
        /// </summary>
        public string Domain { get; } = ConfigurationManager.AppSettings["ida:Domain"];

        /// <summary>
        /// Post logout url to redirect to.
        /// </summary>
        public string PostLogoutRedirectUri { get; } = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        private string authority;

        /// <summary>
        /// Authority constructed from AAD Instance and TenantId
        /// </summary>
        public string Authority { get => authority; }

        public AuthenticationConfig()
        {
            this.authority = CommonUtil.EnsureAuthorityIsV2($"{CommonUtil.EnsureTrailingSlash(AADInstance)}{TenantId}/v2.0");
        }
    }
}