using System;
using System.Collections.Generic;
using System.Configuration;

namespace Microsoft.Identity.Web.Aspnet
{
    public class JwtBearerOptions
    {
        /// <summary>
        /// The Client ID is used by the application to uniquely identify itself to Azure AD. This is also the default audience
        /// </summary>
        public string ClientId { get; } = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// The ClientSecret is a credential used to authenticate the application to Azure AD. Azure AD supports password and certificate credentials.
        /// </summary>
        public string ClientSecret { get; } = ConfigurationManager.AppSettings["ida:ClientSecret"];

        /// <summary>
        /// The instance of Azure AD the user is trying to sign-in to.
        /// </summary>
        public string AADInstance { get; } = ConfigurationManager.AppSettings["ida:Instance"];

        /// <summary>
        /// The Id of the Azure AD tenant.
        /// </summary>
        public string TenantId { get; } = ConfigurationManager.AppSettings["ida:TenantId"];

        /// <summary>
        /// The domain of the Azure AD tenant.
        /// </summary>
        public string Domain { get; } = ConfigurationManager.AppSettings["ida:Domain"];



        private string authority;

        /// <summary>
        /// Authority constructed from AAD Instance and TenantId.
        /// </summary>
        public string Authority { get => authority; }

        /// <summary>
        /// The valid audiences for this Api. the client id and the "api"//clientId" are automatically added
        /// </summary>
        public IList<string> ValidAudiences { get => validAudiences; set => validAudiences = value; }

        private IList<String> validAudiences = new List<string>();

        public JwtBearerOptions()
        {
            this.ValidAudiences.Add(this.ClientId);
            this.ValidAudiences.Add($"api://{this.ClientId}");
            this.authority = CommonUtil.EnsureAuthorityIsV2($"{CommonUtil.EnsureTrailingSlash(AADInstance)}{TenantId}/v2.0");
        }
    }
}